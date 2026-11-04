using EMS.Application.Dtos.Post;

namespace EMS.Application.Interfaces.Post
{
    public interface IPostService
        : ICatalogService<PostCreateDto, PostUpdateDto, PostDetailDto, PostListItemDto, PostListRequestDto>
    {
    }
}
