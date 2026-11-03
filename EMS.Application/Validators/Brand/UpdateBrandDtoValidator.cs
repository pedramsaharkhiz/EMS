using EMS.Application.Dtos.Brand;
using EMS.Application.Services.Brand;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.Brand
{
    public sealed class UpdateBrandDtoValidator : CatalogUpdateValidator<BrandUpdateDto>
    {
        public UpdateBrandDtoValidator() : base(BrandService.EntityLabel, x => x.Code) { }
    }
}
