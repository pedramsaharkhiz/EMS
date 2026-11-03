using EMS.Application.Validators.RefreshToken;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace EMS.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registers what the Application layer owns: its FluentValidation validators and their language.</summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<LoginRequestDtoValidator>();
            // Messages of validators without an explicit WithMessage (FluentValidation ships Persian translations).
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("fa");
            return services;
        }
    }
}
