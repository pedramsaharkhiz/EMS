using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentType;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Services.EquipmentType
{
    public class EquipmentTypeService : IEquipmentTypeService
    {
        private readonly IEquipmentTypeRepository _repository;
        private readonly IEquipmentCategoryRepository _repositoryCategory;
        private readonly IUnitOfWork _unitOfWork;

        public EquipmentTypeService(IEquipmentTypeRepository repository, IUnitOfWork unitOfWork, IEquipmentCategoryRepository repositoryCategory)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _repositoryCategory = repositoryCategory;
        }

        public async Task<PagedResult<EquipmentTypeListItemDto>> GetPagedAsync(EquipmentTypeListRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _repository.Query(asNoTracking: true);

            query = query.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var search = request.Title.Trim();
                query = query.Where(x => x.Title.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code.Contains(search));
            }

            if (request.EquipmentCategoryId.HasValue)
            {
                var search = request.EquipmentCategoryId.Value;
                query = query.Where(x => x.EquipmentCategoryId == search);
            }

            query = ApplySorting(query, request.SortBy, request.SortDirection);

            var projectedQuery = query.Select(x => new EquipmentTypeListItemDto
            {
                EquipmentTypeId = x.EquipmentTypeId,
                EquipmentCategoryId = x.EquipmentCategoryId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive,
            });

            return await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<EquipmentTypeDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (entity is null)
                return null;

            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentTypeDetailDto> CreateAsync(EquipmentTypeCreateDto request, CancellationToken cancellationToken = default)
        {
            var title = request.Title.Trim();

            var existCategory = await _repositoryCategory.ExistsAsync(x => x.EquipmentCategoryId == request.EquipmentCategoryId, cancellationToken);
            if (!existCategory)
                throw new NotFoundException("دسته‌بندی تجهیزات یافت نشد.");

            var duplicateExists = await _repository.ExistsByTitleAsync(title, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("نوع تجهیز دیگری با این عنوان وجود دارد.");

            var entity = new EMS.Domain.Entities.EquipmentType
            {
                EquipmentCategoryId = request.EquipmentCategoryId,
                Title = title,
                Code = request.Code
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentTypeDetailDto?> UpdateAsync(EquipmentTypeUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(request.EquipmentTypeId, asNoTracking: false, cancellationToken);
            if (entity is null)
                return null;

            if (request.EquipmentCategoryId.HasValue)
            {
                var existCategory = await _repositoryCategory.ExistsAsync(x => x.EquipmentCategoryId == request.EquipmentCategoryId.Value, cancellationToken);
                if (!existCategory)
                    throw new NotFoundException("دسته‌بندی تجهیزات یافت نشد.");
            }

            var title = request.Title?.Length > 1 ? request.Title.Trim() : entity.Title;
            var code = request.Code?.Length > 1 ? request.Code.Trim() : entity.Code;

            var duplicateExists = await _repository.ExistsByTitleAsync(title, exceptId: request.EquipmentTypeId, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("دسته‌بندی دیگری با این عنوان وجود دارد.");

            entity.Title = title;
            entity.IsActive = request.IsActive;
            entity.Code = code;
            entity.EquipmentCategoryId = request.EquipmentCategoryId.HasValue ? request.EquipmentCategoryId.Value : entity.EquipmentCategoryId;

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

        private static IQueryable<EMS.Domain.Entities.EquipmentType> ApplySorting(IQueryable<EMS.Domain.Entities.EquipmentType> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "code" => isDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "equipmentcategoryid" => isDescending ? query.OrderByDescending(x => x.EquipmentCategoryId) : query.OrderBy(x => x.EquipmentCategoryId),
                "createddatetime" => isDescending ? query.OrderByDescending(x => x.CreatedDateTime) : query.OrderBy(x => x.CreatedDateTime),
                _ => isDescending ? query.OrderByDescending(x => x.EquipmentTypeId) : query.OrderBy(x => x.EquipmentTypeId)
            };
        }

        private static EquipmentTypeDetailDto MapToDetailsDto(EMS.Domain.Entities.EquipmentType entity)
        {
            return new EquipmentTypeDetailDto
            {
                EquipmentTypeId = entity.EquipmentTypeId,
                EquipmentCategoryId = entity.EquipmentCategoryId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
            };
        }
    }
}