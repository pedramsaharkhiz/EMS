using EMS.Application.Interfaces.Attachment;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Services.Attachments;
using EMS.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The SQL path of attachment storage (a Stream parameter in, a sequential reader out, both inside the caller's transaction) against a
    /// real SQL Server. Manual: skipped unless EMS_TEST_SQL is set (see SqlServerFactAttribute). It creates the schema with EnsureCreated
    /// on a throwaway database and drops it first, so the database name must contain "test".
    /// </summary>
    public class SqlAttachmentBlobStoreTests
    {
        /// <summary>A stream that does not report its length or support seeking, like a request body.</summary>
        private sealed class ForwardOnlyStream : Stream
        {
            private readonly Stream _inner;
            public ForwardOnlyStream(Stream inner) => _inner = inner;
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private static EMSDbContext NewContext()
        {
            var builder = new SqlConnectionStringBuilder(SqlServerFactAttribute.ConnectionString);
            Assert.Contains("test", builder.InitialCatalog, StringComparison.OrdinalIgnoreCase);
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer(builder.ConnectionString).Options;
            return new EMSDbContext(options, new AuditSaveChangesInterceptor(new FakeCurrentUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(), Microsoft.Extensions.Options.Options.Create(new EMS.Infrastructure.Options.AuditTrailOptions()), Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));
        }

        private sealed class FakeCurrentUser : EMS.Application.Common.Interfaces.ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "test";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        [SqlServerFact]
        public async Task A_file_is_streamed_in_and_out_unchanged_and_the_server_measures_it()
        {
            await using var db = NewContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var bytes = new byte[6 * 1024 * 1024 + 17];
            Random.Shared.NextBytes(bytes);

            var streamId = Guid.NewGuid();
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                db.AttachmentContents.Add(new AttachmentContent { StreamId = streamId, FileName = "big.bin", ContentType = "application/pdf", CreatedByUserId = 1 });
                await db.SaveChangesAsync();

                IAttachmentBlobStore store = new SqlAttachmentBlobStore(db);
                var (size, sha) = await store.WriteAsync(streamId, new ForwardOnlyStream(new MemoryStream(bytes)));
                Assert.Equal(bytes.LongLength, size);
                Assert.Equal(SHA256.HashData(bytes), sha);
                await tx.CommitAsync();
            }

            await using var download = await new SqlAttachmentBlobStore(db).OpenReadAsync(streamId);
            Assert.NotNull(download);
            using var read = new MemoryStream();
            await download!.Content.CopyToAsync(read);
            Assert.Equal(bytes, read.ToArray());

            Assert.Null(await new SqlAttachmentBlobStore(db).OpenReadAsync(Guid.NewGuid()));
        }

        [SqlServerFact]
        public async Task A_rolled_back_upload_leaves_no_file_and_no_row()
        {
            await using var db = NewContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var streamId = Guid.NewGuid();
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                db.AttachmentContents.Add(new AttachmentContent { StreamId = streamId, FileName = "x.pdf", ContentType = "application/pdf", CreatedByUserId = 1 });
                await db.SaveChangesAsync();
                await new SqlAttachmentBlobStore(db).WriteAsync(streamId, new MemoryStream(new byte[1000]));
                // no commit: disposing rolls everything back
            }

            db.ChangeTracker.Clear();
            Assert.False(await db.AttachmentContents.AnyAsync(c => c.StreamId == streamId));
            Assert.Null(await new SqlAttachmentBlobStore(db).OpenReadAsync(streamId));
        }
    }
}
