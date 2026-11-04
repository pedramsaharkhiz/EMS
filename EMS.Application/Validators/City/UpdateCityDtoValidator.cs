using EMS.Application.Dtos.City;
using EMS.Application.Services.City;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.City
{
    public sealed class UpdateCityDtoValidator : CatalogUpdateValidator<CityUpdateDto>
    {
        public UpdateCityDtoValidator() : base(CityService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.ProvinceId).GreaterThan(0).When(x => x.ProvinceId.HasValue).WithMessage("شناسه استان باید بزرگتر از صفر باشد.");
        }
    }
}
