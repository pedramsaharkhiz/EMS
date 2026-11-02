using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Services.SupportingDepartment
{
    public class SupportingDepartmentService : ISupportingDepartmentService
    {
        private readonly ISupportingDepartmentRepository _repository;
        private readonly IRepository<City> _cityRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SupportingDepartmentService(ISupportingDepartmentRepository repository, IRepository<City> cityRepository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _cityRepository = cityRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<SupportingDepartmentListItemDto>> GetPagedAsync(SupportingDepartmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _repository.Query(asNoTracking: true);

            query = query.Where(x => x.IsActive);

            if (request.CityId.HasValue)
            {
                var cityId = request.CityId.Value;
                query = query.Where(x => x.CityId == cityId);
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var search = request.Title.Trim();
                query = query.Where(x => x.Title.Contains(search));
            }


            query = ApplySorting(query, request.SortBy, request.SortDirection);
            var projectedQuery = query.Select(x => new SupportingDepartmentListItemDto
            {
                SupportingDepartmentId = x.SupportingDepartmentId,
                CityId = x.CityId,
                Title = x.Title,
                IsActive = x.IsActive,
            });
            return await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<SupportingDepartmentDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (entity is null)
                return null;
            return MapToDetailsDto(entity);
        }

        public async Task<SupportingDepartmentDetailDto> CreateAsync(SupportingDepartmentCreateDto request, CancellationToken cancellationToken = default)
        {
            var cityExists = await _cityRepository.ExistsAsync(x => x.CityId == request.CityId, cancellationToken);
            if (!cityExists)
                throw new NotFoundException("شهر یافت نشد.");

            var title = request.Title.Trim();

            var duplicateExists = await _repository.ExistsByTitleAsync(title, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("دپارتمان پشتیبانی دیگری با این عنوان وجود دارد.");

            var entity = new EMS.Domain.Entities.SupportingDepartment
            {
                CityId = request.CityId,
                Title = title,
            };
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDetailsDto(entity);
        }

        public async Task<SupportingDepartmentDetailDto?> UpdateAsync(SupportingDepartmentUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(request.SupportingDepartmentId, asNoTracking: false, cancellationToken);
            if (entity is null)
                return null;

            if (request.CityId.HasValue)
            {
                var cityExists = await _cityRepository.ExistsAsync(x => x.CityId == request.CityId.Value, cancellationToken);
                if (!cityExists)
                    throw new NotFoundException("شهر یافت نشد.");
            }

            var title = request.Title?.Length > 1 ? request.Title.Trim() : entity.Title;

            var duplicateExists = await _repository.ExistsByTitleAsync(title, exceptId: request.SupportingDepartmentId, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("دپارتمان پشتیبانی دیگری با این عنوان وجود دارد.");

            entity.CityId = request.CityId.HasValue ? request.CityId.Value : entity.CityId;
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

        private static IQueryable<EMS.Domain.Entities.SupportingDepartment> ApplySorting(IQueryable<EMS.Domain.Entities.SupportingDepartment> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "cityid" => isDescending ? query.OrderByDescending(x => x.CityId) : query.OrderBy(x => x.CityId),
                "createddatetime" => isDescending ? query.OrderByDescending(x => x.CreatedDateTime) : query.OrderBy(x => x.CreatedDateTime),
                _ => isDescending ? query.OrderByDescending(x => x.SupportingDepartmentId) : query.OrderBy(x => x.SupportingDepartmentId)
            };
        }

        private static SupportingDepartmentDetailDto MapToDetailsDto(EMS.Domain.Entities.SupportingDepartment entity)
        {
            return new SupportingDepartmentDetailDto
            {
                SupportingDepartmentId = entity.SupportingDepartmentId,
                CityId = entity.CityId,
                Title = entity.Title,
                IsActive = entity.IsActive,
            };
        }
    }
}