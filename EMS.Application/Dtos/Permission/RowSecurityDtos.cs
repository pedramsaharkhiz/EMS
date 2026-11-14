namespace EMS.Application.Dtos.Permission
{
    public sealed class RowPolicyItemDto
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>The read model the policy protects (Equipment, FailureReport, ...).</summary>
        public string RowType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>Does this policy actually limit the current user?</summary>
        public bool RestrictsYou { get; set; }
    }

    /// <summary>How much of the data the signed-in user sees, and why: the answer to "why can't I see this?".</summary>
    public sealed class MyDataScopeDto
    {
        public long? UserId { get; set; }
        public bool IsUnrestricted { get; set; }
        public long? SupportingDepartmentId { get; set; }
        /// <summary>The reason of the scope, in words.</summary>
        public string Reason { get; set; } = string.Empty;
        public List<RowPolicyItemDto> Policies { get; set; } = new();
    }
}
