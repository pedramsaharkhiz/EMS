using EMS.Application.Dtos.NoteCategory;
using EMS.Application.Services.NoteCategory;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.NoteCategory
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreateNoteCategoryDtoValidator : CatalogCreateValidator<NoteCategoryCreateDto>
    {
        public CreateNoteCategoryDtoValidator() : base(NoteCategoryService.EntityLabel) { }
    }
}
