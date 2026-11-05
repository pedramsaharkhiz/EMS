using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Services.SupportingDepartment;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public sealed class CreateSupportingDepartmentDtoValidator : CatalogCreateValidator<SupportingDepartmentCreateDto>
    {
        public CreateSupportingDepartmentDtoValidator() : base(SupportingDepartmentService.EntityLabel)
        {
            RuleFor(x => x.CityId).GreaterThan(0).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Organizations!).SetValidator(new SupportingDepartmentOrganizationsDtoValidator())
                .When(x => SupportingDepartmentOrganizationsDtoValidator.HasSelection(x.Organizations));
        }
    }
}
