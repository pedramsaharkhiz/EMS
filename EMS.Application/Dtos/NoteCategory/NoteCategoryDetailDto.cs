namespace EMS.Application.Dtos.NoteCategory
{
    public class NoteCategoryDetailDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
