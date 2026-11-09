namespace EMS.Application.Dtos.Organization
{
    /// <summary>Assigns or changes the supporting department of a branch.</summary>
    public sealed class OrganizationSupportingDepartmentUpdateDto
    {
        /// <summary>The supporting department to assign to the branch.</summary>
        public long SupportingDepartmentId { get; set; }

        /// <summary>Allows moving a branch that already belongs to another department.</summary>
        public bool MoveFromOtherDepartments { get; set; }
    }
}
