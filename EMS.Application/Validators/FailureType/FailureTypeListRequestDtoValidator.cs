using EMS.Application.Dtos.FailureType;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.FailureType
{
    public sealed class FailureTypeListRequestDtoValidator : CatalogListRequestValidator<FailureTypeListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
