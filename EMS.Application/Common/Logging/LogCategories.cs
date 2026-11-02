namespace EMS.Application.Common.Logging
{
    /// <summary>
    /// Logger categories (SourceContext) used for routing log events.
    /// Events in these categories are also written to the dedicated security/audit log file.
    /// </summary>
    public static class LogCategories
    {
        public const string Security = "EMS.Security";
        public const string Audit = "EMS.Audit";
    }
}
