using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureCategory;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.FailureCategory
{
    public interface IFailureCategoryService
    {
        Task<PagedResult<FailureCategoryListItemDto>> GetPagedAsync(FailureCategoryListRequestDto request, CancellationToken cancellationToken = default);
        Task<FailureCategoryDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<FailureCategoryDetailDto> CreateAsync(FailureCategoryCreateDto request, CancellationToken cancellationToken = default);
        Task<FailureCategoryDetailDto?> UpdateAsync(FailureCategoryUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
