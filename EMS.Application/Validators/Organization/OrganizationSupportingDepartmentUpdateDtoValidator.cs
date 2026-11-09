using EMS.Application.Dtos.Organization;
using FluentValidation;

namespace EMS.Application.Validators.Organization
{
    /// <summary>Validates the request to assign or change a branch's supporting department.</summary>
    public sealed class OrganizationSupportingDepartmentUpdateDtoValidator : AbstractValidator<OrganizationSupportingDepartmentUpdateDto>
    {
        public OrganizationSupportingDepartmentUpdateDtoValidator()
        {
            RuleFor(x => x.SupportingDepartmentId)
                .GreaterThan(0)
                .WithMessage("شناسه دپارتمان پشتیبانی باید بزرگ‌تر از صفر باشد.");
        }
    }
}
