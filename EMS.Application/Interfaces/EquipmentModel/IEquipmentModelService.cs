using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.EquipmentModel
{
    public interface IEquipmentModelService
    {
        Task<PagedResult<EquipmentModelListItemDto>> GetPagedAsync(EquipmentModelListRequestDto request, CancellationToken cancellationToken = default);
        Task<EquipmentModelDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<EquipmentModelDetailDto> CreateAsync(EquipmentModelCreateDto request, CancellationToken cancellationToken = default);
        Task<EquipmentModelDetailDto?> UpdateAsync( EquipmentModelUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
