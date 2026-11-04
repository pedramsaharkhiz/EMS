using EMS.Application.Dtos.Post;
using EMS.Application.Services.Post;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Post
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdatePostDtoValidator : CatalogUpdateValidator<PostUpdateDto>
    {
        public UpdatePostDtoValidator() : base(PostService.EntityLabel, x => x.Code) { }
    }
}
