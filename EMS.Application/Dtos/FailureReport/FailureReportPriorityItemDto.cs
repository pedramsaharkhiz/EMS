namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>
    /// یک اولویت قابل انتخاب در دراپ‌داون گزارش خرابی.
    /// اولویت‌ها ثابت هستند و همیشه فعال‌اند.
    /// </summary>
    public sealed class FailureReportPriorityItemDto
    {
        public int Id { get; init; }

        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// نام ثابت اولویت در Enum، مانند Low یا Urgent.
        /// </summary>
        public string Code { get; init; } = string.Empty;

        public bool IsActive { get; init; } = true;
    }
}
