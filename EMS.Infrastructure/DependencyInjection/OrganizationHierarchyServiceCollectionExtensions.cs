using EMS.Application.Dtos.OrganizationHierarchy;
using EMS.Application.Interfaces.OrganizationHierarchy;
using EMS.Application.Validators.OrganizationHierarchy;
using EMS.Infrastructure.Services.OrganizationHierarchy;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EMS.Infrastructure.DependencyInjection;

public static class OrganizationHierarchyServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationHierarchy(
        this IServiceCollection services)
    {
        services.TryAddScoped<IValidator<OrganizationHierarchyRequestDto>,OrganizationHierarchyRequestDtoValidator>();

        services.AddScoped<
            IOrganizationHierarchyService,
            OrganizationHierarchyService>();

        return services;
    }
}
