namespace EMS.Application.Dtos.Post
{
    public class PostListItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }
}
