using EMS.Application.Common.Export;
using EMS.Domain.ReadModels;

namespace EMS.Application.Services.User
{
    /// <summary>
    /// The columns of the users Excel file: what the users list shows (no password, no security data). It reads the same rows and filters as
    /// the list. Codes, national code and phone are text so that leading zeros are kept.
    /// </summary>
    public static class UserExport
    {
        public static readonly ExcelExportDefinition<UserRow> Definition = ExcelExportDefinition
            .For<UserRow>(name: "Users", sheetTitle: "کاربران", fileLabel: "کاربران")
            .Column(x => x.Id)
            .Column(x => x.UserName, "نام کاربری", ExcelCellKind.Text)
            .Column(x => x.FirstName, "نام")
            .Column(x => x.LastName, "نام خانوادگی")
            .Column(x => x.PersonnelCode, "کد پرسنلی", ExcelCellKind.Text)
            .Column(x => x.NationalCode, "کد ملی", ExcelCellKind.Text)
            .Column(x => x.PhoneNumber, "تلفن", ExcelCellKind.Text)
            .Column(x => x.Email, "ایمیل", ExcelCellKind.Text)
            .Column(x => x.PostTitle, "سمت")
            .Column(x => x.SupportingDepartmentTitle, "دپارتمان پشتیبانی")
            .Column(x => x.CityTitle, "شهر")
            .Column(x => x.ProvinceTitle, "استان")
            .Column(x => x.RoleNames, "نقش‌ها")
            .Column(x => x.SkillTitles, "مهارت‌ها")
            .Column(x => x.IsActive)
            .Build();
    }
}
