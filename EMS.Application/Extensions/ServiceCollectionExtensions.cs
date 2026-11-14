using EMS.Application.Validators.RefreshToken;
using EMS.Application.Services.Equipment;
using EMS.Application.Common.DataAccess;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace EMS.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers every concrete <see cref="IRowPolicy{TRow}"/> of <paramref name="assembly"/> (as its policy interface and as an
        /// <see cref="IRowPolicyDescriptor"/> for the explanation screen). A new policy class needs no registration line.
        /// </summary>
        public static IServiceCollection AddRowPolicies(this IServiceCollection services, System.Reflection.Assembly assembly)
        {
            foreach (var implementation in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
            {
                var policyInterfaces = implementation.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRowPolicy<>)).ToList();
                if (policyInterfaces.Count == 0)
                    continue;

                services.AddScoped(implementation);
                foreach (var policyInterface in policyInterfaces)
                    services.AddScoped(policyInterface, sp => sp.GetRequiredService(implementation));
                services.AddScoped<IRowPolicyDescriptor>(sp => (IRowPolicyDescriptor)sp.GetRequiredService(implementation));
            }
            return services;
        }

        /// <summary>Registers what the Application layer owns: AutoMapper profiles, FluentValidation validators and their language.</summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(new[] { "EMS.Domain", "EMS.Infrastructure", "EMS.Application" });
            });
            services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
            services.AddScoped<EMS.Application.Interfaces.AssignmentNote.IAssignmentNoteAccessPolicy,
                EMS.Application.Services.AssignmentNote.ParticipantAssignmentNoteAccessPolicy>();
            // A technician is allowed only at a supporting department.
            services.AddScoped<IEquipmentLocationRule, DepartmentTechnicianLocationRule>();
            // Row-level security: the single door to protected rows, and the policies (one class per protected read model) found in this assembly.
            services.AddScoped<IRowAccess, RowAccess>();
            services.AddRowPolicies(typeof(ServiceCollectionExtensions).Assembly);
            // The files of a record are visible exactly when the record is: one guard per attachable table (found in this assembly).
            foreach (var guard in typeof(ServiceCollectionExtensions).Assembly.GetTypes()
                         .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(EMS.Application.Interfaces.Attachment.IAttachmentOwnerGuard).IsAssignableFrom(t)))
                services.AddScoped(typeof(EMS.Application.Interfaces.Attachment.IAttachmentOwnerGuard), guard);
            // Messages of validators without an explicit WithMessage (FluentValidation ships Persian translations).
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("fa");
            return services;
        }
    }
}
