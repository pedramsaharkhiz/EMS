using EMS.Application.Common.Models;
using EMS.Application.Dtos.Brand;
using EMS.Application.Dtos.EquipmentCategory;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.Brand
{
    public interface IBrandService
    {
        Task<PagedResult<BrandListItemDto>> GetPagedAsync(BrandListRequestDto request, CancellationToken cancellationToken = default);
        Task<BrandDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BrandDetailDto> CreateAsync(BrandCreateDto request, CancellationToken cancellationToken = default);
        Task<BrandDetailDto?> UpdateAsync( BrandUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
