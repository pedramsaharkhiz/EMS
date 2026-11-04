using EMS.Application.Dtos.ActionEntity;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.ActionEntity
{
    public sealed class ActionEntityListRequestDtoValidator : CatalogListRequestValidator<ActionEntityListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
