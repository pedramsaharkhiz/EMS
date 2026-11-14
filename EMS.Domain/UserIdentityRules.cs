using System.Text.RegularExpressions;

namespace EMS.Domain;

/// <summary>Canonical user identifiers. Keep strings so leading zeros survive.</summary>
public static class UserIdentityRules
{
    public const string PersonnelIndex = "UX_AspNetUsers_PersonnelCode";
    public const string NationalIndex = "UX_AspNetUsers_NationalCode";
    public const string PersonnelTaken = "این کد پرسنلی قبلاً برای کاربر دیگری ثبت شده است.";
    public const string NationalTaken = "این کد ملی قبلاً برای کاربر دیگری ثبت شده است.";

    public static string Normalize(string? value) => new((value ?? "").Trim().Select(c => c switch
    {
        >= '۰' and <= '۹' => (char)('0' + c - '۰'),
        >= '٠' and <= '٩' => (char)('0' + c - '٠'),
        _ => c
    }).ToArray());

    public static string? NormalizeOptional(string? value) => Normalize(value) is { Length: > 0 } code ? code : null;

    public static bool HasNationalFormat(string? value) =>
        Regex.IsMatch(Normalize(value), @"\A[0-9]{10}\z", RegexOptions.CultureInvariant);

    public static bool IsValidNationalCode(string? value)
    {
        var code = Normalize(value);
        if (!HasNationalFormat(code) || code.All(c => c == code[0])) return false;
        var remainder = Enumerable.Range(0, 9).Sum(i => (code[i] - '0') * (10 - i)) % 11;
        return code[9] - '0' == (remainder < 2 ? remainder : 11 - remainder);
    }
}
