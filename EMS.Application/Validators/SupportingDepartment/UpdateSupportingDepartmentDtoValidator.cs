using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Services.SupportingDepartment;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public sealed class UpdateSupportingDepartmentDtoValidator : CatalogUpdateValidator<SupportingDepartmentUpdateDto>
    {
        public UpdateSupportingDepartmentDtoValidator() : base(SupportingDepartmentService.EntityLabel)
        {
            RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
        }
    }
}
