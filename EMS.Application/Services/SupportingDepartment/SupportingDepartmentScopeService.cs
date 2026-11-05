using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Domain.Interfaces;

namespace EMS.Application.Services.SupportingDepartment
{
    /// <summary>
    /// Which branches (شعبه) a supporting department serves. One branch has at most one department, so the department id lives on the
    /// branch itself. A request can select branches one by one and by group (field, branch management, zone); a group means all of its
    /// ACTIVE branches at the moment of the request (a branch added to the group later is not picked up automatically).
    /// All or nothing: a refused selection changes nothing. No geographic rule (city or province) is applied: the business has not asked
    /// for one and a branch has no city or province.
    /// </summary>
    public sealed class SupportingDepartmentScopeService : ISupportingDepartmentScopeService
    {
        private const int MaxDetails = 50;

        private readonly ISupportingDepartmentRepository _departments;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;

        public SupportingDepartmentScopeService(ISupportingDepartmentRepository departments, IUnitOfWork unitOfWork, IAuditEventLogger audit)
        {
            _departments = departments;
            _unitOfWork = unitOfWork;
            _audit = audit;
        }

        public async Task<SupportingDepartmentOrganizationsResultDto> AssignAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default)
        {
            await RequireDepartmentAsync(departmentId, mustBeActive: true, cancellationToken);
            var organizations = await PrepareAssignmentAsync(departmentId, request, string.Empty, cancellationToken);

            int assigned = 0, moved = 0, unchanged = 0;
            foreach (var organization in organizations)
            {
                if (organization.SupportingDepartmentId == departmentId) { unchanged++; continue; }
                if (organization.SupportingDepartmentId.HasValue) moved++; else assigned++;
                organization.SupportingDepartmentId = departmentId;
            }

            if (assigned + moved > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _audit.Operation("OrganizationsAssigned", "SupportingDepartment", departmentId, $"assigned={assigned}, moved={moved}");
            }

            return new SupportingDepartmentOrganizationsResultDto
            {
                Selected = organizations.Count, Assigned = assigned, Moved = moved, Unchanged = unchanged,
                TotalOrganizations = await _departments.CountOrganizationsAsync(departmentId, cancellationToken),
            };
        }

        public async Task<IReadOnlyList<EMS.Domain.Entities.Organization>> PrepareAssignmentAsync(long? departmentId, SupportingDepartmentOrganizationsDto request, string fieldPrefix, CancellationToken cancellationToken = default)
        {
            var selected = await SelectAsync(request, explicitMustBeActive: true, fieldPrefix, cancellationToken);
            var organizations = await _departments.GetOrganizationsForUpdateAsync(selected, cancellationToken);

            var others = organizations.Where(o => o.SupportingDepartmentId.HasValue && o.SupportingDepartmentId != departmentId).ToList();
            if (others.Count > 0 && !request.MoveFromOtherDepartments)
                throw await ConflictAsync(others, fieldPrefix, cancellationToken);

            return organizations;
        }

        public async Task<SupportingDepartmentOrganizationsResultDto> RemoveAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default)
        {
            await RequireDepartmentAsync(departmentId, mustBeActive: false, cancellationToken);
            var selected = await SelectAsync(request, explicitMustBeActive: false, string.Empty, cancellationToken);
            var organizations = await _departments.GetOrganizationsForUpdateAsync(selected, cancellationToken);

            var removed = 0;
            foreach (var organization in organizations.Where(o => o.SupportingDepartmentId == departmentId))
            {
                organization.SupportingDepartmentId = null;
                removed++;
            }

            if (removed > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _audit.Operation("OrganizationsRemoved", "SupportingDepartment", departmentId, $"removed={removed}");
            }

            return new SupportingDepartmentOrganizationsResultDto
            {
                Selected = organizations.Count, Removed = removed, Unchanged = organizations.Count - removed,
                TotalOrganizations = await _departments.CountOrganizationsAsync(departmentId, cancellationToken),
            };
        }

        // ------------------------------------------------------------------------------------------------ helpers

        private async Task RequireDepartmentAsync(long id, bool mustBeActive, CancellationToken cancellationToken)
        {
            if (!await _departments.ExistsIncludingInactiveAsync(x => x.SupportingDepartmentId == id, cancellationToken))
                throw new NotFoundException("دپارتمان پشتیبانی موردنظر پیدا نشد.", ErrorCodes.NotFound);
            if (mustBeActive && !await _departments.ExistsAsync(x => x.SupportingDepartmentId == id, cancellationToken))
                throw new BadRequestException("دپارتمان پشتیبانی انتخاب‌شده غیرفعال است.", ErrorCodes.ParentInactive);
        }

        /// <summary>
        /// The distinct branch ids the request selects. Every id must exist (a group id must name a real field, management or zone;
        /// a branch id a real branch, and for assigning an active one); problems are reported together, per item.
        /// </summary>
        private async Task<List<long>> SelectAsync(SupportingDepartmentOrganizationsDto request, bool explicitMustBeActive, string fieldPrefix, CancellationToken cancellationToken)
        {
            var organizationIds = request.OrganizationIds ?? new List<long>();
            var fieldIds = request.FieldIds ?? new List<long>();
            var managementIds = request.OrganizationManagementIds ?? new List<long>();
            var zoneIds = request.ZoneIds ?? new List<long>();

            var problems = new List<ApiErrorDetail>();
            void Missing(string field, List<long> ids, HashSet<long> existing, string label)
            {
                for (var i = 0; i < ids.Count; i++)
                    if (!existing.Contains(ids[i]))
                        problems.Add(new ApiErrorDetail { Field = $"{fieldPrefix}{field}[{i}]", Message = $"{label} با شناسه {ids[i]} یافت نشد." });
            }

            var existingOrganizations = await _departments.GetExistingIdsAsync("organization", organizationIds, cancellationToken);
            Missing("organizationIds", organizationIds, existingOrganizations, "شعبه");
            Missing("fieldIds", fieldIds, await _departments.GetExistingIdsAsync("field", fieldIds, cancellationToken), "ناحیه");
            Missing("organizationManagementIds", managementIds, await _departments.GetExistingIdsAsync("organizationmanagement", managementIds, cancellationToken), "مدیریت شعب");
            Missing("zoneIds", zoneIds, await _departments.GetExistingIdsAsync("zone", zoneIds, cancellationToken), "حوزه");

            var selected = organizationIds.Where(existingOrganizations.Contains).ToHashSet();
            if (explicitMustBeActive && selected.Count > 0)
            {
                var rows = await _departments.GetOrganizationsForUpdateAsync(selected.ToList(), cancellationToken);
                var inactive = rows.Where(o => !o.IsActive).Select(o => o.OrganizationId).ToHashSet();
                for (var i = 0; i < organizationIds.Count; i++)
                    if (inactive.Contains(organizationIds[i]))
                        problems.Add(new ApiErrorDetail { Field = $"{fieldPrefix}organizationIds[{i}]", Message = $"شعبه با شناسه {organizationIds[i]} غیرفعال است." });
                selected.ExceptWith(inactive);
            }

            if (problems.Count > 0)
                throw new BulkItemsException(problems.Take(MaxDetails).ToList(), $"{problems.Count} مورد از انتخاب نامعتبر است و هیچ تغییری انجام نشد.");

            selected.UnionWith(await _departments.ResolveOrganizationIdsAsync(fieldIds, managementIds, zoneIds, cancellationToken));
            return selected.ToList();
        }

        private async Task<ConflictItemsException> ConflictAsync(List<EMS.Domain.Entities.Organization> others, string fieldPrefix, CancellationToken cancellationToken)
        {
            var titles = await _departments.GetTitlesAsync(others.Select(o => o.SupportingDepartmentId!.Value).ToList(), cancellationToken);
            var details = others.Take(MaxDetails).Select(o => new ApiErrorDetail
            {
                Field = fieldPrefix + "organizations",
                Message = $"شعبه «{o.Title}» (شناسه {o.OrganizationId}) از قبل زیر دپارتمان «{titles.GetValueOrDefault(o.SupportingDepartmentId!.Value, string.Empty)}» است.",
            }).ToList();

            return new ConflictItemsException(details,
                $"{others.Count} شعبه‌ی انتخاب‌شده از قبل زیر دپارتمان دیگری هستند و هیچ تغییری انجام نشد. برای جابه‌جایی «moveFromOtherDepartments: true» را بفرستید.",
                ErrorCodes.OrganizationsAlreadyAssigned);
        }
    }
}
