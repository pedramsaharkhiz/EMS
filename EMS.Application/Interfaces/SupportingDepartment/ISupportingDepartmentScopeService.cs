using EMS.Application.Dtos.Organization;
using EMS.Application.Dtos.SupportingDepartment;

namespace EMS.Application.Interfaces.SupportingDepartment
{
    /// <summary>Manages the branches a supporting department serves through Organization.SupportingDepartmentId.</summary>
    public interface ISupportingDepartmentScopeService
    {
        /// <summary>
        /// Validates a selection and returns tracked branches without saving, so department create and update can save everything together.
        /// <paramref name="departmentId"/> is null for a new department. <paramref name="fieldPrefix"/> prefixes error field names.
        /// </summary>
        Task<IReadOnlyList<EMS.Domain.Entities.Organization>> PrepareAssignmentAsync(long? departmentId, SupportingDepartmentOrganizationsDto request, string fieldPrefix, CancellationToken cancellationToken = default);

        /// <summary>Puts the selected branches under the department, all or nothing.</summary>
        Task<SupportingDepartmentOrganizationsResultDto> AssignAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default);

        /// <summary>Removes selected branches from this department; branches belonging to other departments remain unchanged.</summary>
        Task<SupportingDepartmentOrganizationsResultDto> RemoveAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default);

        /// <summary>Returns the branch and its current supporting department; department fields are null when unassigned.</summary>
        Task<OrganizationSupportingDepartmentDto> GetOrganizationDepartmentAsync(long organizationId, CancellationToken cancellationToken = default);

        /// <summary>Assigns or changes a branch's department using the existing department-side assignment rules.</summary>
        Task<OrganizationSupportingDepartmentDto> SetOrganizationDepartmentAsync(long organizationId, OrganizationSupportingDepartmentUpdateDto request, CancellationToken cancellationToken = default);

        /// <summary>Removes the relationship without deleting either entity. An unassigned branch remains unchanged.</summary>
        Task RemoveOrganizationDepartmentAsync(long organizationId, CancellationToken cancellationToken = default);
    }
}
