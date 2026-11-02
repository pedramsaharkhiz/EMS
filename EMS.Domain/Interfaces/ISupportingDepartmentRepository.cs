using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface ISupportingDepartmentRepository : IRepository<SupportingDepartment> 
    {
        Task<SupportingDepartment?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default);
    }
}
