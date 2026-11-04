using EMS.Application.Dtos.Province;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Province
{
    public sealed class ProvinceListRequestDtoValidator : CatalogListRequestValidator<ProvinceListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
