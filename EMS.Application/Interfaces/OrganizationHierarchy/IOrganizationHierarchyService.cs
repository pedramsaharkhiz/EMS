using EMS.Application.Dtos.OrganizationHierarchy;

namespace EMS.Application.Interfaces.OrganizationHierarchy;

public interface IOrganizationHierarchyService
{
    Task<OrganizationHierarchyResult> GetAsync(
        OrganizationHierarchyRequestDto request,
        CancellationToken cancellationToken = default);
}
