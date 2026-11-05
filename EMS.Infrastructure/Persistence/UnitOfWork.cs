using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EMSDbContext _context;

        public UnitOfWork(EMSDbContext context)
        {
            _context = context;
        }

        // Counted here (not read from the context) because a provider that ignores transactions (the in-memory one) has no CurrentTransaction.
        private int _openTransactions;

        public bool HasActiveTransaction => _openTransactions > 0;

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _openTransactions++;
            return new EfTransaction(transaction, () => _openTransactions--);
        }

        private sealed class EfTransaction : IUnitOfWorkTransaction
        {
            private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;
            private readonly Action _closed;
            private bool _done;
            public EfTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, Action closed)
            {
                _transaction = transaction;
                _closed = closed;
            }
            public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);
            public async ValueTask DisposeAsync()
            {
                if (_done) return;
                _done = true;
                try { await _transaction.DisposeAsync(); }   // rolls back when not committed
                finally { _closed(); }
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The row was changed (or removed) by someone else after it was read.
                throw new ConflictException(RowVersionToken.ConflictMessage, ErrorCodes.ConcurrencyConflict);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                throw new ConflictException("رکوردی با مقدار یکتا مشابه از قبل وجود دارد.", ErrorCodes.UniqueViolation);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                // Foreign key violation: a referenced record does not exist, or the record is still referenced.
                throw new ConflictException("عملیات به دلیل وجود رکوردهای وابسته یا ارجاع به رکورد نامعتبر امکان‌پذیر نیست.", ErrorCodes.ForeignKeyViolation);
            }
        }
    }
}