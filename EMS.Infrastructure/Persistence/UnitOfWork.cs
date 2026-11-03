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