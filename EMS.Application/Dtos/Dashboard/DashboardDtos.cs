namespace EMS.Application.Dtos.Dashboard
{
    /// <summary>One bar of a dashboard card: a status row and how many records the caller sees in it (zero is kept so an empty bar can be drawn).</summary>
    public class DashboardStatusCountDto
    {
        public long Id { get; set; }
        /// <summary>The stable code of a system status; null for a status an administrator added.</summary>
        public string? Code { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class EquipmentStatusCountDto : DashboardStatusCountDto
    {
        /// <summary>The status is one of "نیازمند اقدام" (under maintenance, out of service, damaged). A status an administrator added is never one.</summary>
        public bool NeedsAction { get; set; }
    }

    public sealed class FailureReportStatusCountDto : DashboardStatusCountDto
    {
        /// <summary>Every status except "بسته" (closed).</summary>
        public bool IsUnresolved { get; set; }
    }

    public sealed class AssignmentStatusCountDto : DashboardStatusCountDto
    {
        /// <summary>Assigned, accepted or in progress.</summary>
        public bool IsOpen { get; set; }
    }

    public sealed class DashboardOwnerCountDto
    {
        /// <summary>The value of the <c>owner</c> filter of the equipment list.</summary>
        public int Owner { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class DashboardPriorityCountDto
    {
        /// <summary>The value of the <c>priorityId</c> filter of the failure report list (1 to 5).</summary>
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>"دارایی‌های ثبت‌شده": the active equipment the caller sees. The sum of <see cref="ByStatus"/> and of <see cref="ByOwner"/> is always <see cref="Total"/>.</summary>
    public sealed class EquipmentFleetSummaryDto
    {
        public int Total { get; set; }
        public int NeedsActionCount { get; set; }
        public List<EquipmentStatusCountDto> ByStatus { get; set; } = new();
        public List<DashboardOwnerCountDto> ByOwner { get; set; } = new();
    }

    public sealed class FailureReportDashboardDto
    {
        public int Total { get; set; }
        /// <summary>Active reports whose status is not "بسته".</summary>
        public int UnresolvedCount { get; set; }
        /// <summary>Unresolved reports that have no open assignment (work not given to anyone yet).</summary>
        public int UnresolvedWithoutAssignmentCount { get; set; }
        public List<FailureReportStatusCountDto> ByStatus { get; set; } = new();
        /// <summary>The unresolved reports per priority; all five priorities are listed, even with zero.</summary>
        public List<DashboardPriorityCountDto> UnresolvedByPriority { get; set; } = new();
    }

    public sealed class AssignmentDashboardDto
    {
        public int Total { get; set; }
        public int OpenCount { get; set; }
        /// <summary>Open with a deadline in the past (the same meaning as <c>isOverdue</c> and the <c>overdue</c> filter).</summary>
        public int OverdueCount { get; set; }
        /// <summary>Open assignments the technician has not seen yet.</summary>
        public int UnseenCount { get; set; }
        /// <summary>The open assignments per priority; all five priorities are listed, even with zero.</summary>
        public List<DashboardPriorityCountDto> OpenByPriority { get; set; } = new();
        public List<AssignmentStatusCountDto> ByStatus { get; set; } = new();
    }
}
