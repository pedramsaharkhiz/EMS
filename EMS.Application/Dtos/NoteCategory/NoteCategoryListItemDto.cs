namespace EMS.Application.Dtos.NoteCategory
{
    public class NoteCategoryListItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
