using EMS.Application.Common.Export;
using EMS.Domain.ReadModels;

namespace EMS.Application.Services.FailureReport
{
    /// <summary>
    /// The columns of the failure report Excel file: what the list shows, in the same order of meaning. It reads the same
    /// <see cref="FailureReportRow"/> as the list and the detail, so the file shows what the grid shows. To add a column, add the
    /// property to the row (and its projection in the repository) and one line here.
    /// </summary>
    public static class FailureReportExport
    {
        public static readonly ExcelExportDefinition<FailureReportRow> Definition = ExcelExportDefinition
            .For<FailureReportRow>(name: "FailureReport", sheetTitle: "گزارش‌های خرابی", fileLabel: "گزارش-خرابی")
            .Column(x => x.Id)
            .Column(x => x.ReferenceNumber, "شماره ارجاع", ExcelCellKind.Text)
            .Column(x => x.StatusTitle, "وضعیت گزارش")
            .Column(x => x.PriorityTitle, "اولویت")
            .Column(x => x.ReportTypeTitle, "نوع گزارش")
            .Column(x => x.FailureCategoryTitle, "دسته‌بندی خرابی")
            .Column(x => x.FailureTypeTitle, "نوع خرابی")
            .Column(x => x.EquipmentYaasCode, "یاس کد تجهیز", ExcelCellKind.Text)
            .Column(x => x.EquipmentSerialNumber, "سریال نامبر", ExcelCellKind.Text)
            .Column(x => x.EquipmentAssetNumber, "شماره دارایی", ExcelCellKind.Text)
            .Column(x => x.EquipmentOwnerTitle, "مالک تجهیز")
            .Column(x => x.EquipmentTypeTitle, "نوع تجهیز")
            .Column(x => x.EquipmentModelTitle, "مدل تجهیز")
            .Column(x => x.EquipmentStatusTitle, "وضعیت تجهیز")
            .Column(x => x.OrganizationTitle, "شعبه")
            .Column(x => x.SupportingDepartmentTitle, "دپارتمان پشتیبانی")
            .Column(x => x.EmployeeName, "کارمند")
            .Column(x => x.EmployeePersonnelCode, "کد پرسنلی کارمند", ExcelCellKind.Text)
            .Column(x => x.ReportedByName, "ثبت‌کننده")
            .Column(x => x.ReportedByPersonnelCode, "کد پرسنلی ثبت‌کننده", ExcelCellKind.Text)
            .Column(x => x.ReportedByPostTitle, "سمت ثبت‌کننده")
            .Column(x => x.ReportedBySupportingDepartmentTitle, "دپارتمان ثبت‌کننده")
            .Column(x => x.ReportedByCityTitle, "شهر ثبت‌کننده")
            .Column(x => x.ReportedByProvinceTitle, "استان ثبت‌کننده")
            .Column(x => x.ReportedDateTime, "زمان گزارش")
            .Column(x => x.Description)
            .Column(x => x.IsActive)
            .Column(x => x.CreatedDateTime)
            .Build();
    }
}
