namespace EMS.Application.Dtos.SupportingDepartment
{
    /// <summary>
    /// Selects branches for a supporting department, one by one and/or by group. The result is the union of: the listed branches,
    /// every ACTIVE branch under the listed fields (حوزه), under the listed branch managements (مدیریت شعب) and under
    /// the listed zones (ناحیه). At least one list must have a value.
    /// </summary>
    public class SupportingDepartmentOrganizationsDto
    {
        public List<long>? OrganizationIds { get; set; }
        public List<long>? FieldIds { get; set; }
        public List<long>? OrganizationManagementIds { get; set; }
        public List<long>? ZoneIds { get; set; }

        /// <summary>
        /// Assign only. false (default): a branch that already belongs to another department refuses the whole request (409, nothing
        /// changes). true: those branches are moved to this department.
        /// </summary>
        public bool MoveFromOtherDepartments { get; set; }
    }

    /// <summary>What an assign or remove did. <see cref="Selected"/> is the number of distinct branches the request selected.</summary>
    public class SupportingDepartmentOrganizationsResultDto
    {
        public int Selected { get; set; }
        /// <summary>Assign: branches that had no department and now belong to this one.</summary>
        public int Assigned { get; set; }
        /// <summary>Assign: branches moved here from another department (only with <c>moveFromOtherDepartments</c>).</summary>
        public int Moved { get; set; }
        /// <summary>Assign: branches that already belonged to this department. Remove: selected branches that did not belong to it.</summary>
        public int Unchanged { get; set; }
        /// <summary>Remove: branches taken off this department.</summary>
        public int Removed { get; set; }
        /// <summary>How many active branches the department serves now.</summary>
        public int TotalOrganizations { get; set; }
    }
}
