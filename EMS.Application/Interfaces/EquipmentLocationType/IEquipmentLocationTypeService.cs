using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.EquipmentLocationType
{
    public interface IEquipmentLocationTypeService
    {
        Task<PagedResult<EquipmentLocationTypeListItemDto>> GetPagedAsync(EquipmentLocationTypeListRequestDto request, CancellationToken cancellationToken = default);
        Task<EquipmentLocationTypeDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<EquipmentLocationTypeDetailDto> CreateAsync(EquipmentLocationTypeCreateDto request, CancellationToken cancellationToken = default);
        Task<EquipmentLocationTypeDetailDto?> UpdateAsync( EquipmentLocationTypeUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
