using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.EquipmentType
{
    public interface IEquipmentTypeService
    {
        Task<PagedResult<EquipmentTypeListItemDto>> GetPagedAsync(EquipmentTypeListRequestDto request, CancellationToken cancellationToken = default);
        Task<EquipmentTypeDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<EquipmentTypeDetailDto> CreateAsync(EquipmentTypeCreateDto request, CancellationToken cancellationToken = default);
        Task<EquipmentTypeDetailDto?> UpdateAsync( EquipmentTypeUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
