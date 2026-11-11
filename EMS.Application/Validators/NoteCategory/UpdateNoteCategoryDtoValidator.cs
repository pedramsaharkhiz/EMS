using EMS.Application.Dtos.NoteCategory;
using EMS.Application.Services.NoteCategory;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.NoteCategory
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdateNoteCategoryDtoValidator : CatalogUpdateValidator<NoteCategoryUpdateDto>
    {
        public UpdateNoteCategoryDtoValidator() : base(NoteCategoryService.EntityLabel) { }
    }
}
