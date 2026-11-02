using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentStatus;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.EquipmentStatus
{
    public interface IEquipmentStatusService
    {
        Task<PagedResult<EquipmentStatusListItemDto>> GetPagedAsync(EquipmentStatusListRequestDto request, CancellationToken cancellationToken = default);
        Task<EquipmentStatusDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<EquipmentStatusDetailDto> CreateAsync(EquipmentStatusCreateDto request, CancellationToken cancellationToken = default);
        Task<EquipmentStatusDetailDto?> UpdateAsync( EquipmentStatusUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
