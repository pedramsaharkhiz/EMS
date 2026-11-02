using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.SupportingDepartment;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.SupportingDepartment
{
    public interface ISupportingDepartmentService
    {
        Task<PagedResult<SupportingDepartmentListItemDto>> GetPagedAsync(SupportingDepartmentListRequestDto request, CancellationToken cancellationToken = default);
        Task<SupportingDepartmentDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<SupportingDepartmentDetailDto> CreateAsync(SupportingDepartmentCreateDto request, CancellationToken cancellationToken = default);
        Task<SupportingDepartmentDetailDto?> UpdateAsync( SupportingDepartmentUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
