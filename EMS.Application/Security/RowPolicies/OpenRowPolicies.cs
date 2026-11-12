using EMS.Application.Common.DataAccess;
using EMS.Domain.ReadModels;

namespace EMS.Application.Security.RowPolicies
{
    // One policy per protected read model. They are all OPEN for now (no restriction): the rule that will limit what a user sees has not been decided yet
    // (docs/RLS-FINDINGS.md). The types are registered so that they show up in the explanation screen and so that a restricted scope has a policy to ask; when the
    // rule is decided, only the body of the matching class changes: Filter() returns the condition and Describe() says it in words.

    /// <summary>Equipment (list, detail, export, dashboard, history, technician, attachments, move).</summary>
    public sealed class EquipmentRowPolicy : OpenRowPolicy<EquipmentRow>
    {
        public override string Name => "Equipment";
    }

    /// <summary>Failure reports (list, detail, export, dashboard, update, delete, assignments of a report).</summary>
    public sealed class FailureReportRowPolicy : OpenRowPolicy<FailureReportRow>
    {
        public override string Name => "FailureReport";
    }

    /// <summary>Technician assignments (list, detail, actions, transitions, dashboard). An assignment is visible through its report, or to its own technician.</summary>
    public sealed class TechnicianAssignmentRowPolicy : OpenRowPolicy<TechnicianAssignmentRow>
    {
        public override string Name => "TechnicianAssignment";
    }
}
