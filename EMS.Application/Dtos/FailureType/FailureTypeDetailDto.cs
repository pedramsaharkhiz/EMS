namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeDetailDto
    {
        public long Id { get; set; }
        public long FailureCategoryId { get; set; }
        public string? FailureCategoryTitle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
