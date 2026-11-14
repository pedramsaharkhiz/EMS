using System.Text.RegularExpressions;

namespace EMS.Domain;

public static class EmployeePhoneRules
{
    public const int MaxPerReportRequest = 20;
    public const string UniqueIndex = "UX_EmployeePhoneNumbers_PhoneNumber";
    public const string TakenCode = "employee_phone_number_taken";
    public const string TakenMessage = "این شماره موبایل قبلاً ثبت شده است و نمی‌تواند برای کارمند دیگری ثبت شود.";
    public static string Normalize(string? value)
    {
        var phone = UserIdentityRules.Normalize(value);
        if (phone.StartsWith("+98", StringComparison.Ordinal)) return "0" + phone[3..];
        if (phone.StartsWith("0098", StringComparison.Ordinal)) return "0" + phone[4..];
        return phone;
    }
    public static bool IsValid(string? value) => Regex.IsMatch(Normalize(value), @"\A09[0-9]{9}\z", RegexOptions.CultureInvariant);
}
