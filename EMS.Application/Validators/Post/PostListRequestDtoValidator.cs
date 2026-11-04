using EMS.Application.Dtos.Post;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Post
{
    public sealed class PostListRequestDtoValidator : CatalogListRequestValidator<PostListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
