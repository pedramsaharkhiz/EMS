using EMS.Application.Dtos.NoteCategory;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.NoteCategory
{
    public sealed class NoteCategoryListRequestDtoValidator : CatalogListRequestValidator<NoteCategoryListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
