using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.Post
{
    public class PostCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
