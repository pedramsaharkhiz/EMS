using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Domain.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Extensions;

namespace EMS.Application.Services.EquipmentCategory
{
    public class EquipmentCategoryService : IEquipmentCategoryService
    {
        private readonly IEquipmentCategoryRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public EquipmentCategoryService(IEquipmentCategoryRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<EquipmentCategoryListItemDto>> GetPagedAsync(EquipmentCategoryListRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _repository.Query(asNoTracking: true);

            query = query.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var search = request.Title.Trim();
                query = query.Where(x => x.Title.Contains(search));
            }
            query = ApplySorting(query, request.SortBy, request.SortDirection);
            var projectedQuery = query.Select(x => new EquipmentCategoryListItemDto
            {
                EquipmentCategoryId = x.EquipmentCategoryId,
                Title = x.Title,
            });
            return await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<EquipmentCategoryDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (entity is null)
                return null;
            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentCategoryDetailDto> CreateAsync(EquipmentCategoryCreateDto request, CancellationToken cancellationToken = default)
        {
            var title = request.Title.Trim();
            var duplicateExists = await _repository.ExistsByTitleAsync(title, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("دسته‌بندی دیگری با این عنوان وجود دارد.");

            var entity = new EMS.Domain.Entities.EquipmentCategory
            {
                Title = title,
                IsActive = true,
            };
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentCategoryDetailDto?> UpdateAsync(EquipmentCategoryUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(request.EquipmentCategoryId, asNoTracking: false, cancellationToken);
            if (entity is null)
                return null;

            var title = request.Title?.Length > 1 ? request.Title.Trim() : entity.Title;
            var duplicateExists = await _repository.ExistsByTitleAsync(title, exceptId: request.EquipmentCategoryId, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("دسته‌بندی دیگری با این عنوان وجود دارد.");

            entity.Title = title;
            entity.IsActive = request.IsActive;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDetailsDto(entity);
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (entity is null)
                return false;

            entity.IsActive = false;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }


        private static IQueryable<EMS.Domain.Entities.EquipmentCategory> ApplySorting(IQueryable<EMS.Domain.Entities.EquipmentCategory> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "createddatetime" => isDescending ? query.OrderByDescending(x => x.CreatedDateTime) : query.OrderBy(x => x.CreatedDateTime),
                _ => isDescending ? query.OrderByDescending(x => x.EquipmentCategoryId) : query.OrderBy(x => x.EquipmentCategoryId)
            };
        }

        private static EquipmentCategoryDetailDto MapToDetailsDto(EMS.Domain.Entities.EquipmentCategory entity)
        {
            return new EquipmentCategoryDetailDto
            {
                EquipmentCategoryId = entity.EquipmentCategoryId,
                Title = entity.Title,
                IsActive = entity.IsActive,
            };
        }
    }
}