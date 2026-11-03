using EMS.Application.Dtos.City;
using EMS.Application.Services.City;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.City
{
    public sealed class CreateCityDtoValidator : CatalogCreateValidator<CityCreateDto>
    {
        public CreateCityDtoValidator() : base(CityService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.ProvinceId).GreaterThan(0).WithMessage("شناسه استان باید بزرگتر از صفر باشد.");
        }
    }
}
