namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeListItemDto
    {
        public long Id { get; set; }
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }
}
