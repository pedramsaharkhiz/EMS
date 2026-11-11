namespace EMS.Domain
{
    /// <summary>Column lengths and constraint names of the technician assignment tables, shared by the EF configuration and the validators.</summary>
    public static class TechnicianAssignmentConstraints
    {
        public const int DescriptionMaxLength = 500;
        public const int NoteMaxLength = 500;
        /// <summary>A technician has at most one open assignment per report (a report may have several open ones, one per technician).</summary>
        public const string OneOpenPerReportAndTechnicianIndexName = "UX_TechnicianAssignments_OpenPerReportAndTechnician";

        /// <summary>The most assignments one bulk request may create (the technicians of one failure).</summary>
        public const int BulkMaxItems = 20;
    }
}
