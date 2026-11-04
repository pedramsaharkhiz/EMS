using EMS.Application.Dtos.SupportingDepartment;

namespace EMS.Application.Interfaces.SupportingDepartment
{
    /// <summary>The branches a supporting department serves (<c>Organization.SupportingDepartmentId</c>).</summary>
    public interface ISupportingDepartmentScopeService
    {
        /// <summary>Puts the selected branches under the department (all or nothing).</summary>
        Task<SupportingDepartmentOrganizationsResultDto> AssignAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default);

        /// <summary>Takes the selected branches off the department; branches that belong to another department are left alone.</summary>
        Task<SupportingDepartmentOrganizationsResultDto> RemoveAsync(long departmentId, SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken = default);
    }
}
