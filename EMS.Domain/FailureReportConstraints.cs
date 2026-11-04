namespace EMS.Domain
{
    /// <summary>Column lengths of <c>FailureReport</c>, shared by the EF configuration and the validators.</summary>
    public static class FailureReportConstraints
    {
        public const int ReferenceNumberMaxLength = 50;
        public const int DescriptionMaxLength = 500;

        /// <summary>How far ahead of the server clock a reported time may be (clock differences between client and server).</summary>
        public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);
    }
}
