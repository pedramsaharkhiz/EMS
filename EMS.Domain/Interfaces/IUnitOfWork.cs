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
        /// <summary>Is a transaction started with <see cref="BeginTransactionAsync"/> open right now (so a nested piece of work must not start its own)?</summary>
        bool HasActiveTransaction { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Starts a database transaction for work that spans several saves (e.g. an attachment row and its file stream).</summary>
        Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
