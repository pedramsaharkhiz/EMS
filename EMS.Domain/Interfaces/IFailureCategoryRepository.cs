using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IFailureCategoryRepository : IRepository<FailureCategory> 
    {
        Task<FailureCategory?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default);
        /// <summary>True when active records still depend on this one (so it must not be deactivated).</summary>
        Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default);
    }
}
