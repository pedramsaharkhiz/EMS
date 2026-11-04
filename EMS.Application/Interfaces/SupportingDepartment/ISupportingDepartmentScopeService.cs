using EMS.Application.Dtos.SupportingDepartment;

namespace EMS.Application.Interfaces.SupportingDepartment
{
    /// <summary>The branches a supporting department serves (<c>Organization.SupportingDepartmentId</c>).</summary>
    public interface ISupportingDepartmentScopeService
    {
        /// <summary>
        /// Validates a selection for a department and returns the branches (tracked) to put under it, without saving: used by the
        /// create and update of the department itself so that everything is saved together. <paramref name="departmentId"/> is null for a
        /// department that does not exist yet. <paramref name="fieldPrefix"/> prefixes the field names of the errors (e.g. "organizations.").
        /// </summary>
        Task<IReadOnlyList<EMS.Domain.Entities.Organization>> PrepareAssignmentAsync(long? departmentId, SupportingDepartmentOrganizationsDto request, string fieldPrefix, CancellationToken cancellationToken = default);

        /// <summary>Puts the selected branches under the department (all or nothing).</summary>
        Task<SupportingDepartmentOrganizationsResultDto> AssignAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default);

        /// <summary>Takes the selected branches off the department; branches that belong to another department are left alone.</summary>
        Task<SupportingDepartmentOrganizationsResultDto> RemoveAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default);
    }
}
