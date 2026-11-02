using EMS.Application.Dtos.Brand;
using EMS.Application.Services.Brand;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.Brand
{
    public sealed class BrandListRequestDtoValidator : CatalogListRequestValidator<BrandListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
