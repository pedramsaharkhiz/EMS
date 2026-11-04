using EMS.Application.Dtos.Post;
using EMS.Application.Services.Post;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Post
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreatePostDtoValidator : CatalogCreateValidator<PostCreateDto>
    {
        public CreatePostDtoValidator() : base(PostService.EntityLabel, x => x.Code) { }
    }
}
