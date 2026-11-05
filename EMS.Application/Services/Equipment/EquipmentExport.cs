using EMS.Application.Common.Export;
using EMS.Domain.ReadModels;

namespace EMS.Application.Services.Equipment
{
    /// <summary>
    /// The columns of the equipment Excel file. It reads the same <see cref="EquipmentRow"/> as the list, so the file shows what
    /// the list shows. To add a column, add the property to the row (and its projection in the repository) and one line here.
    /// </summary>
    public static class EquipmentExport
    {
        public static readonly ExcelExportDefinition<EquipmentRow> Definition = ExcelExportDefinition
            .For<EquipmentRow>(name: "Equipment", sheetTitle: "تجهیزات", fileLabel: "تجهیزات")
            .Column(x => x.Id)
            .Column(x => x.OwnerTitle, "مالک")
            .Column(x => x.YaasCode, "یاس کد", ExcelCellKind.Text)
            .Column(x => x.SerialNumber, "سریال نامبر", ExcelCellKind.Text)
            .Column(x => x.AssetNumber, "شماره دارایی", ExcelCellKind.Text)
            .Column(x => x.CategoryTitle, "دسته‌بندی")
            .Column(x => x.EffectiveTypeTitle, "نوع تجهیز")
            .Column(x => x.EquipmentModelTitle, "مدل")
            .Column(x => x.BrandTitle, "برند")
            .Column(x => x.StatusTitle, "وضعیت")
            .Column(x => x.LocationTypeTitle, "نوع مکان")
            .Column(x => x.OrganizationTitle, "سازمان")
            .Column(x => x.ZoneTitle, "حوزه")
            .Column(x => x.OrganizationManagementTitle, "مدیریت شعب")
            .Column(x => x.FieldTitle, "ناحیه")
            .Column(x => x.OrganizationSupportingDepartmentTitle, "دپارتمان پشتیبانی شعبه")
            .Column(x => x.SupportingDepartmentTitle, "دپارتمان پشتیبانی")
            .Column(x => x.CityTitle, "شهر")
            .Column(x => x.ProvinceTitle, "استان")
            .Column(x => x.TechnicianName, "تکنسین")
            .Column(x => x.TechnicianSupportingDepartmentTitle, "دپارتمان تکنسین")
            .Column(x => x.TechnicianCityTitle, "شهر تکنسین")
            .Column(x => x.TechnicianProvinceTitle, "استان تکنسین")
            .Column(x => x.Description)
            .Column(x => x.IsActive)
            .Column(x => x.CreatedDateTime)
            .Build();
    }
}
