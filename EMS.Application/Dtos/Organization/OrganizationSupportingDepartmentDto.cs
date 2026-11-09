namespace EMS.Application.Dtos.Organization
{
    /// <summary>A branch and its current supporting department relationship.</summary>
    public sealed class OrganizationSupportingDepartmentDto
    {
        public long OrganizationId { get; init; }

        public string OrganizationTitle { get; init; } = string.Empty;

        /// <summary>Null when the branch has no supporting department.</summary>
        public long? SupportingDepartmentId { get; init; }

        /// <summary>Null when the branch has no supporting department.</summary>
        public string? SupportingDepartmentTitle { get; init; }
    }
}
