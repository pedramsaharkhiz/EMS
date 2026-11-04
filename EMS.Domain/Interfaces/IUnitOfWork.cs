using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IUnitOfWorkTransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Starts a database transaction for work that spans several saves (e.g. an attachment row and its file stream).</summary>
        Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
