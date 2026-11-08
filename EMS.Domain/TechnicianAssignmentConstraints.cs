namespace EMS.Domain
{
    /// <summary>Column lengths and constraint names of the technician assignment tables, shared by the EF configuration and the validators.</summary>
    public static class TechnicianAssignmentConstraints
    {
        public const int DescriptionMaxLength = 500;
        public const int NoteMaxLength = 500;
        public const string OneOpenPerReportIndexName = "UX_TechnicianAssignments_OpenPerReport";
    }
}
