namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>The list item plus what is needed to edit: the modification time and the <see cref="RowVersion"/>.</summary>
    public class FailureReportDetailDto : FailureReportListItemDto
    {
        public List<EMS.Application.Dtos.EmployeePhoneNumber.EmployeePhoneNumberItemDto> EmployeePhoneNumbers { get; set; } = [];
        public DateTime? ModifiedDateTime { get; set; }
        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
