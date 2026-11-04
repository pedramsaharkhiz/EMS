using EMS.Application.Interfaces.Attachment;
using EMS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EMS.Infrastructure.Services.Attachments
{
    /// <summary>
    /// Keeps the bytes of attachments in <c>AttachmentContents.Content</c> (varbinary(max)) of the application's own SQL Server database.
    /// Both directions are streams: an upload is sent to the server as a <see cref="Stream"/> parameter (the file is never held in memory and
    /// never touches the app server's disk), a download is read with a sequential-access reader. Plain SQL Server, no FILESTREAM needed.
    /// </summary>
    public sealed class SqlAttachmentBlobStore : IAttachmentBlobStore
    {
        private readonly EMSDbContext _context;

        public SqlAttachmentBlobStore(EMSDbContext context)
        {
            _context = context;
        }

        public async Task<(long SizeBytes, byte[] Sha256)> WriteAsync(Guid streamId, Stream content, CancellationToken cancellationToken = default)
        {
            var connection = (SqlConnection)_context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            // joins the transaction the caller started, so the file and its rows commit (or roll back) together
            var transaction = _context.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction;

            await using (var write = new SqlCommand("UPDATE [AttachmentContents] SET [Content] = @content WHERE [StreamId] = @id", connection, transaction))
            {
                write.Parameters.Add(new SqlParameter("@content", SqlDbType.VarBinary, -1) { Value = content });
                write.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = streamId });
                var rows = await write.ExecuteNonQueryAsync(cancellationToken);
                if (rows != 1)
                    throw new InvalidOperationException($"The attachment content row {streamId} does not exist.");
            }

            // the server measures and hashes what it stored, so the numbers are those of the stored file
            await using var measure = new SqlCommand(
                "UPDATE [AttachmentContents] SET [SizeBytes] = DATALENGTH([Content]), [Sha256] = HASHBYTES('SHA2_256', [Content]) " +
                "OUTPUT inserted.[SizeBytes], inserted.[Sha256] WHERE [StreamId] = @id", connection, transaction);
            measure.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = streamId });
            await using var reader = await measure.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return (reader.GetInt64(0), (byte[])reader.GetValue(1));
        }

        public async Task<AttachmentDownload?> OpenReadAsync(Guid streamId, CancellationToken cancellationToken = default)
        {
            // its own connection: the stream outlives the request's DbContext work and is read while the response is written
            var connection = new SqlConnection(_context.Database.GetConnectionString());
            SqlCommand? command = null;
            SqlDataReader? reader = null;
            try
            {
                await connection.OpenAsync(cancellationToken);
                command = new SqlCommand("SELECT [Content] FROM [AttachmentContents] WHERE [StreamId] = @id", connection);
                command.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = streamId });
                reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow, cancellationToken);

                if (!await reader.ReadAsync(cancellationToken) || await reader.IsDBNullAsync(0, cancellationToken))
                {
                    await Release(reader, command, connection);
                    return null;
                }

                return new AttachmentDownload(reader.GetStream(0), new Owner(reader, command, connection));
            }
            catch
            {
                await Release(reader, command, connection);
                throw;
            }
        }

        private static async Task Release(SqlDataReader? reader, SqlCommand? command, SqlConnection connection)
        {
            if (reader is not null) await reader.DisposeAsync();
            if (command is not null) await command.DisposeAsync();
            await connection.DisposeAsync();
        }

        private sealed class Owner : IAsyncDisposable
        {
            private readonly SqlDataReader _reader;
            private readonly SqlCommand _command;
            private readonly SqlConnection _connection;

            public Owner(SqlDataReader reader, SqlCommand command, SqlConnection connection)
            {
                _reader = reader;
                _command = command;
                _connection = connection;
            }

            public ValueTask DisposeAsync() => new(Release(_reader, _command, _connection));
        }
    }
}
