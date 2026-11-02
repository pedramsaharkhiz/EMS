using EMS.Application.Dtos.Brand;
using EMS.Application.Services.Brand;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.Brand
{
    public sealed class CreateBrandDtoValidator : CatalogCreateValidator<BrandCreateDto>
    {
        public CreateBrandDtoValidator() : base(BrandService.EntityLabel, x => x.Code) { }
    }
}
