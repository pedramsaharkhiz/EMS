using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentModel;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Services.EquipmentModel
{
    public class EquipmentModelService : IEquipmentModelService
    {
        private readonly IEquipmentModelRepository _repository;
        private readonly IEquipmentTypeRepository _repositoryType;
        private readonly IBrandRepository _repositoryBrand;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;

        public EquipmentModelService(IEquipmentModelRepository repository, IUnitOfWork unitOfWork, IEquipmentTypeRepository repositoryType, IBrandRepository repositoryBrand, IAuditEventLogger audit)
        {
            _audit = audit;
            _repository = repository;
            _unitOfWork = unitOfWork;
            _repositoryType = repositoryType;
            _repositoryBrand = repositoryBrand;
        }

        public async Task<PagedResult<EquipmentModelListItemDto>> GetPagedAsync(EquipmentModelListRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _repository.Query(asNoTracking: true);

            // Active records by default; ?isActive=false lists the deactivated ones (so they can be found and restored).
            var wantActive = request.IsActive ?? true;
            query = query.Where(x => x.IsActive == wantActive);

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

            if (request.EquipmentTypeId.HasValue)
            {
                var search = request.EquipmentTypeId.Value;
                query = query.Where(x => x.EquipmentTypeId == search);
            }

            if (request.BrandId.HasValue)
            {
                var search = request.BrandId.Value;
                query = query.Where(x => x.BrandId == search);
            }

            query = ApplySorting(query, request.SortBy, request.SortDirection);

            var projectedQuery = query.Select(x => new EquipmentModelListItemDto
            {
                EquipmentModelId = x.EquipmentModelId,
                EquipmentTypeId = x.EquipmentTypeId,
                BrandId = x.BrandId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive,
            });

            return await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<EquipmentModelDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (entity is null)
                return null;

            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentModelDetailDto> CreateAsync(EquipmentModelCreateDto request, CancellationToken cancellationToken = default)
        {
            var title = request.Title.Trim();

            var existType = await _repositoryType.ExistsAsync(x => x.EquipmentTypeId == request.EquipmentTypeId, cancellationToken);
            if (!existType)
                throw new NotFoundException("نوع تجهیزات یافت نشد.");

            var existBrand = await _repositoryBrand.ExistsAsync(x => x.BrandId == request.BrandId, cancellationToken);
            if (!existBrand)
                throw new NotFoundException("برند یافت نشد.");

            if (!await _repositoryType.ExistsAsync(x => x.EquipmentTypeId == request.EquipmentTypeId && x.IsActive, cancellationToken))
                throw new BadRequestException("نوع تجهیزات انتخاب‌شده غیرفعال است.");

            if (!await _repositoryBrand.ExistsAsync(x => x.BrandId == request.BrandId && x.IsActive, cancellationToken))
                throw new BadRequestException("برند انتخاب‌شده غیرفعال است.");

            var duplicateExists = await _repository.ExistsByTitleAsync(title, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("مدل تجهیز دیگری با این عنوان وجود دارد.");

            var entity = new EMS.Domain.Entities.EquipmentModel
            {
                EquipmentTypeId = request.EquipmentTypeId,
                BrandId = request.BrandId,
                Title = title,
                Code = request.Code
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _audit.Created("EquipmentModel", entity.EquipmentModelId);

            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentModelDetailDto?> UpdateAsync(EquipmentModelUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(request.EquipmentModelId, asNoTracking: false, cancellationToken);
            if (entity is null)
                return null;

            if (request.EquipmentTypeId.HasValue)
            {
                var existType = await _repositoryType.ExistsAsync(x => x.EquipmentTypeId == request.EquipmentTypeId.Value, cancellationToken);
                if (!existType)
                    throw new NotFoundException("نوع تجهیزات یافت نشد.");
            }

            if (request.BrandId.HasValue)
            {
                var existBrand = await _repositoryBrand.ExistsAsync(x => x.BrandId == request.BrandId.Value, cancellationToken);
                if (!existBrand)
                    throw new NotFoundException("برند یافت نشد.");
            }

            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;

            var targetTypeId = request.EquipmentTypeId ?? entity.EquipmentTypeId;
            if ((targetTypeId != entity.EquipmentTypeId || reactivating) &&
                !await _repositoryType.ExistsAsync(x => x.EquipmentTypeId == targetTypeId && x.IsActive, cancellationToken))
                throw new BadRequestException("نوع تجهیزات انتخاب‌شده غیرفعال است.");

            var targetBrandId = request.BrandId ?? entity.BrandId;
            if ((targetBrandId != entity.BrandId || reactivating) &&
                !await _repositoryBrand.ExistsAsync(x => x.BrandId == targetBrandId && x.IsActive, cancellationToken))
                throw new BadRequestException("برند انتخاب‌شده غیرفعال است.");

            var title = request.Title?.Length > 1 ? request.Title.Trim() : entity.Title;
            var code = request.Code?.Length > 1 ? request.Code.Trim() : entity.Code;

            var duplicateExists = await _repository.ExistsByTitleAsync(title, exceptId: request.EquipmentModelId, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("مدل تجهیز دیگری با این عنوان وجود دارد.");

            entity.Title = title;
            entity.Code = code;
            if (RowVersionToken.TryParse(request.RowVersion, out var expectedVersion))
                _repository.SetExpectedModifiedDateTime(entity, expectedVersion);

            if (entity.IsActive && !request.IsActive && await _repository.HasActiveDependentsAsync(entity.EquipmentModelId, cancellationToken))
                throw new ConflictException("این مدل توسط تجهیز فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.");

            entity.IsActive = request.IsActive;
            entity.EquipmentTypeId = request.EquipmentTypeId.HasValue ? request.EquipmentTypeId.Value : entity.EquipmentTypeId;
            entity.BrandId = request.BrandId.HasValue ? request.BrandId.Value : entity.BrandId;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _audit.Updated("EquipmentModel", entity.EquipmentModelId);

            return MapToDetailsDto(entity);
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: false, cancellationToken);
            if (entity is null)
                return false;

            if (entity.IsActive && await _repository.HasActiveDependentsAsync(id, cancellationToken))
                throw new ConflictException("این مدل توسط تجهیز فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.");

            entity.IsActive = false;
            _repository.Update(entity);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _audit.Deleted("EquipmentModel", entity.EquipmentModelId);

            return true;
        }

        private static IQueryable<EMS.Domain.Entities.EquipmentModel> ApplySorting(IQueryable<EMS.Domain.Entities.EquipmentModel> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "code" => isDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "equipmenttypeid" => isDescending ? query.OrderByDescending(x => x.EquipmentTypeId) : query.OrderBy(x => x.EquipmentTypeId),
                "brandid" => isDescending ? query.OrderByDescending(x => x.BrandId) : query.OrderBy(x => x.BrandId),
                "createddatetime" => isDescending ? query.OrderByDescending(x => x.CreatedDateTime) : query.OrderBy(x => x.CreatedDateTime),
                _ => isDescending ? query.OrderByDescending(x => x.EquipmentModelId) : query.OrderBy(x => x.EquipmentModelId)
            };
        }

        private static EquipmentModelDetailDto MapToDetailsDto(EMS.Domain.Entities.EquipmentModel entity)
        {
            return new EquipmentModelDetailDto
            {
                EquipmentModelId = entity.EquipmentModelId,
                EquipmentTypeId = entity.EquipmentTypeId,
                BrandId = entity.BrandId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
        }
    }
}