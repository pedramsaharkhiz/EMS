using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The valid priorities of a failure report and their Persian titles (shown as <c>priorityTitle</c>).</summary>
    public static class FailureReportPriorities
    {
        public const int Min = (int)FailureReportPriority.Low;
        public const int Max = (int)FailureReportPriority.Urgent;

        private static readonly IReadOnlyDictionary<FailureReportPriority, string> Titles = new Dictionary<FailureReportPriority, string>
        {
            [FailureReportPriority.Low] = "کم",
            [FailureReportPriority.Medium] = "متوسط",
            [FailureReportPriority.High] = "بالا",
            [FailureReportPriority.VeryHigh] = "خیلی بالا",
            [FailureReportPriority.Urgent] = "فوری",
        };

        public static bool IsValid(int priorityId) => priorityId >= Min && priorityId <= Max;

        /// <summary>The Persian title of a priority number, or the number itself when it is not a known priority.</summary>
        public static string TitleOf(int priorityId) =>
            Titles.TryGetValue((FailureReportPriority)priorityId, out var title) ? title : priorityId.ToString();
    }
}
