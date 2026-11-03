using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Services.SupportingDepartment;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public sealed class SupportingDepartmentListRequestDtoValidator : CatalogListRequestValidator<SupportingDepartmentListRequestDto>
    {
        public SupportingDepartmentListRequestDtoValidator() : base()
        {
            RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
            RuleFor(x => x.ProvinceId).GreaterThan(0).When(x => x.ProvinceId.HasValue).WithMessage("شناسه استان باید بزرگتر از صفر باشد.");
        }
    }
}
