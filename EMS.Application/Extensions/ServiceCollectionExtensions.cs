using EMS.Application.Validators.RefreshToken;
using EMS.Application.Services.Equipment;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace EMS.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
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
            // Messages of validators without an explicit WithMessage (FluentValidation ships Persian translations).
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("fa");
            return services;
        }
    }
}
