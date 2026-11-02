using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.EquipmentCategory
{
    public interface IEquipmentCategoryService
    {
        Task<PagedResult<EquipmentCategoryListItemDto>> GetPagedAsync(EquipmentCategoryListRequestDto request, CancellationToken cancellationToken = default);
        Task<EquipmentCategoryDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<EquipmentCategoryDetailDto> CreateAsync(EquipmentCategoryCreateDto request, CancellationToken cancellationToken = default);
        Task<EquipmentCategoryDetailDto?> UpdateAsync(EquipmentCategoryUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
