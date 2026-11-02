using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentStatus;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Services.EquipmentStatus
{
    public class EquipmentStatusService : IEquipmentStatusService
    {
        private readonly IEquipmentStatusRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public EquipmentStatusService(IEquipmentStatusRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<EquipmentStatusListItemDto>> GetPagedAsync(EquipmentStatusListRequestDto request, CancellationToken cancellationToken = default)
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
            var projectedQuery = query.Select(x => new EquipmentStatusListItemDto
            {
                EquipmentStatusId = x.EquipmentStatusId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive,
            });
            return await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<EquipmentStatusDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, asNoTracking: true, cancellationToken);
            if (entity is null)
                return null;
            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentStatusDetailDto> CreateAsync(EquipmentStatusCreateDto request, CancellationToken cancellationToken = default)
        {
            var title = request.Title.Trim();

            var duplicateExists = await _repository.ExistsByTitleAsync(title, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("وضعیت تجهیز دیگری با این عنوان وجود دارد.");

            var entity = new EMS.Domain.Entities.EquipmentStatus
            {
                Title = title,
                Code = request.Code
            };
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDetailsDto(entity);
        }

        public async Task<EquipmentStatusDetailDto?> UpdateAsync(EquipmentStatusUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(request.EquipmentStatusId, asNoTracking: false, cancellationToken);
            if (entity is null)
                return null;

            var title = request.Title?.Length > 1 ? request.Title.Trim() : entity.Title;
            var code = request.Code?.Length > 1 ? request.Code.Trim() : entity.Code;

            var duplicateExists = await _repository.ExistsByTitleAsync(title, exceptId: request.EquipmentStatusId, cancellationToken: cancellationToken);
            if (duplicateExists)
                throw new ConflictException("وضعیت تجهیز دیگری با این عنوان وجود دارد.");

            entity.Title = title;
            entity.IsActive = request.IsActive;
            entity.Code = code;
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

        private static IQueryable<EMS.Domain.Entities.EquipmentStatus> ApplySorting(IQueryable<EMS.Domain.Entities.EquipmentStatus> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
            return normalizedSortBy switch
            {
                "title" => isDescending ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
                "code" => isDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "createddatetime" => isDescending ? query.OrderByDescending(x => x.CreatedDateTime) : query.OrderBy(x => x.CreatedDateTime),
                _ => isDescending ? query.OrderByDescending(x => x.EquipmentStatusId) : query.OrderBy(x => x.EquipmentStatusId)
            };
        }

        private static EquipmentStatusDetailDto MapToDetailsDto(EMS.Domain.Entities.EquipmentStatus entity)
        {
            return new EquipmentStatusDetailDto
            {
                EquipmentStatusId = entity.EquipmentStatusId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
            };
        }
    }
}