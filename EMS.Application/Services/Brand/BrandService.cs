using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.Brand;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.Brand;

namespace EMS.Application.Services.Brand
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;

        public BrandService(IBrandRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
        {
            _audit = audit;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<BrandListItemDto>> GetPagedAsync(BrandListRequestDto request, CancellationToken cancellationToken = default)
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

            query = ApplySorting(query, request.SortBy, request.SortDirection);
            var projectedQuery = query.Select(x => new BrandListItemDto
            {
                BrandId = x.BrandId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive,
            });
            return await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<BrandDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (entity is null)
                return null;
            return MapToDetailsDto(entity);
        }

        public async Task<BrandDetailDto> CreateAsync(BrandCreateDto request, CancellationToken cancellationToken = default)
        {
            var title = request.Title.Trim();

            var duplicateExists = await _repository.ExistsByTitleAsync(title, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("برند دیگری با این عنوان وجود دارد.");

            var entity = new EMS.Domain.Entities.Brand
            {
                Title = title,
                Code = request.Code
            };
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _audit.Created("Brand", entity.BrandId);
            return MapToDetailsDto(entity);
        }

        public async Task<BrandDetailDto?> UpdateAsync(BrandUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(request.BrandId, asNoTracking: false, cancellationToken);
            if (entity is null)
                return null;

            var title = request.Title?.Length > 1 ? request.Title.Trim() : entity.Title;
            var code = request.Code?.Length > 1 ? request.Code.Trim() : entity.Code;

            var duplicateExists = await _repository.ExistsByTitleAsync(title, exceptId: request.BrandId, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("برند دیگری با این عنوان وجود دارد.");

            entity.Title = title;
            entity.Code = code;
            entity.IsActive = request.IsActive;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _audit.Updated("Brand", entity.BrandId);
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
            _audit.Deleted("Brand", entity.BrandId);
            return true;
        }

        //private long GetCurrentUserId()
        //{
        //    var userId = _currentUserService.UserId;
        //    if (!userId.HasValue || userId.Value <= 0)
        //        throw new UnauthorizedAccessException("کاربر جاری شناسایی نشد.");

        //    return userId.Value;
        //}

        private static IQueryable<EMS.Domain.Entities.Brand> ApplySorting(IQueryable<EMS.Domain.Entities.Brand> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            return normalizedSortBy switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "code" => isDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "createddatetime" => isDescending ? query.OrderByDescending(x => x.CreatedDateTime) : query.OrderBy(x => x.CreatedDateTime),
                _ => isDescending ? query.OrderByDescending(x => x.BrandId) : query.OrderBy(x => x.BrandId)
            };
        }

        private static BrandDetailDto MapToDetailsDto(EMS.Domain.Entities.Brand entity)
        {
            return new BrandDetailDto
            {
                BrandId = entity.BrandId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
            };
        }
    }
}