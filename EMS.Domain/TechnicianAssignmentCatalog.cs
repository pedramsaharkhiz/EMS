using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The seeded rows of <c>TechnicianAssignmentStatuses</c> and the allowed status changes of an assignment.</summary>
    public static class TechnicianAssignmentCatalog
    {
        public static readonly IReadOnlyDictionary<TechnicianAssignmentStatusCode, string> Titles = new Dictionary<TechnicianAssignmentStatusCode, string>
        {
            [TechnicianAssignmentStatusCode.Assigned] = "ارجاع‌شده",
            [TechnicianAssignmentStatusCode.Accepted] = "پذیرفته‌شده",
            [TechnicianAssignmentStatusCode.Rejected] = "ردشده",
            [TechnicianAssignmentStatusCode.InProgress] = "در حال انجام",
            [TechnicianAssignmentStatusCode.Completed] = "تکمیل‌شده",
            [TechnicianAssignmentStatusCode.Cancelled] = "لغوشده",
        };

        /// <summary>The statuses of an assignment that is still alive (one report has at most one of them).</summary>
        public static readonly IReadOnlyList<TechnicianAssignmentStatusCode> OpenCodes = new[]
        {
            TechnicianAssignmentStatusCode.Assigned, TechnicianAssignmentStatusCode.Accepted, TechnicianAssignmentStatusCode.InProgress,
        };

        /// <summary>The ids of <see cref="OpenCodes"/>, for queries.</summary>
        public static readonly long[] OpenIds = OpenCodes.Select(IdOf).ToArray();

        private static readonly IReadOnlyDictionary<TechnicianAssignmentStatusCode, TechnicianAssignmentStatusCode[]> Transitions =
            new Dictionary<TechnicianAssignmentStatusCode, TechnicianAssignmentStatusCode[]>
            {
                [TechnicianAssignmentStatusCode.Assigned] = new[] { TechnicianAssignmentStatusCode.Accepted, TechnicianAssignmentStatusCode.Rejected, TechnicianAssignmentStatusCode.Cancelled },
                [TechnicianAssignmentStatusCode.Accepted] = new[] { TechnicianAssignmentStatusCode.InProgress, TechnicianAssignmentStatusCode.Cancelled },
                [TechnicianAssignmentStatusCode.InProgress] = new[] { TechnicianAssignmentStatusCode.Completed, TechnicianAssignmentStatusCode.Cancelled },
            };

        public static long IdOf(TechnicianAssignmentStatusCode code) => (long)code;

        public static string TitleOf(TechnicianAssignmentStatusCode code) => Titles.TryGetValue(code, out var title) ? title : string.Empty;

        public static bool IsOpen(long statusId) => OpenIds.Contains(statusId);

        public static bool CanMove(TechnicianAssignmentStatusCode from, TechnicianAssignmentStatusCode to) =>
            Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}
