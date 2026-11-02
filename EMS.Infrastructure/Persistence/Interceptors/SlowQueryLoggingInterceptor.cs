using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace EMS.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Logs database commands that take longer than a configurable threshold.
    /// Only the SQL text and timing are logged - never parameter values.
    /// </summary>
    public sealed class SlowQueryLoggingInterceptor : DbCommandInterceptor
    {
        private const int MaxSqlLength = 2000;

        private readonly ILogger<SlowQueryLoggingInterceptor> _logger;
        private readonly TimeSpan _threshold;

        public SlowQueryLoggingInterceptor(ILogger<SlowQueryLoggingInterceptor> logger, int thresholdMs)
        {
            _logger = logger;
            _threshold = TimeSpan.FromMilliseconds(thresholdMs);
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            LogIfSlow(command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            LogIfSlow(command, eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            LogIfSlow(command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            LogIfSlow(command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
        {
            LogIfSlow(command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        {
            LogIfSlow(command, eventData);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
        {
            if (eventData.Duration < _threshold)
                return;

            var sql = command.CommandText;
            if (sql.Length > MaxSqlLength)
                sql = sql[..MaxSqlLength] + "…";

            _logger.LogWarning(
                "Slow database command ({ElapsedMs} ms, threshold {ThresholdMs} ms): {CommandText}",
                (long)eventData.Duration.TotalMilliseconds, (long)_threshold.TotalMilliseconds, sql);
        }
    }
}
