using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EmployeePhoneNumber;
using EMS.Application.Validators.Catalog;
using EMS.Domain;
using FluentValidation;
using FluentValidation.Results;

namespace EMS.Application.Validators.EmployeePhoneNumber;

public static class EmployeePhoneInputRules
{
    public const string InvalidPhone = "شماره موبایل معتبر نیست؛ نمونهٔ صحیح: 09123456789";
    public static bool ValidVersion(string? value)
    {
        try { RowVersionToken.TryParse(value, out _); return true; }
        catch (BadRequestException) { return false; }
    }
    public static List<ValidationFailure> Errors(long? employeeId, List<EmployeePhoneNumberInputDto>? items)
    {
        var errors = new List<ValidationFailure>();
        if (items is null || items.Count == 0) return errors;
        if (employeeId is null or <= 0) errors.Add(new("EmployeeId", "برای ثبت شماره‌ها، انتخاب کارمند الزامی است."));
        if (items.Count > EmployeePhoneRules.MaxPerReportRequest) errors.Add(new("EmployeePhoneNumbers", "در هر درخواست حداکثر ۲۰ شماره مجاز است."));
        var ids = new HashSet<long>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i]; var path = $"EmployeePhoneNumbers[{i}]";
            if (item is null) { errors.Add(new(path, "اطلاعات شماره موبایل الزامی است.")); continue; }
            if (!EmployeePhoneRules.IsValid(item.PhoneNumber)) errors.Add(new(path + ".PhoneNumber", InvalidPhone));
            if (item.Id is { } id && (id <= 0 || !ids.Add(id))) errors.Add(new(path + ".Id", "شناسه شماره باید مثبت و بدون تکرار باشد."));
            if (!ValidVersion(item.RowVersion)) errors.Add(new(path + ".RowVersion", "نسخهٔ رکورد معتبر نیست."));
        }
        return errors;
    }
}
public sealed class EmployeePhoneNumberCreateDtoValidator : AbstractValidator<EmployeePhoneNumberCreateDto>
{
    public EmployeePhoneNumberCreateDtoValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("شناسه کارمند باید بزرگتر از صفر باشد.");
        RuleFor(x => x.PhoneNumber).Must(EmployeePhoneRules.IsValid).WithMessage(EmployeePhoneInputRules.InvalidPhone);
    }
}
public sealed class EmployeePhoneNumberUpdateDtoValidator : AbstractValidator<EmployeePhoneNumberUpdateDto>
{
    public EmployeePhoneNumberUpdateDtoValidator()
    {
        RuleFor(x => x.PhoneNumber).Must(EmployeePhoneRules.IsValid).WithMessage(EmployeePhoneInputRules.InvalidPhone);
        RuleFor(x => x.RowVersion).Must(EmployeePhoneInputRules.ValidVersion).WithMessage("نسخهٔ رکورد معتبر نیست.");
    }
}
public sealed class EmployeePhoneNumberListRequestDtoValidator : CatalogListRequestValidator<EmployeePhoneNumberListRequestDto>
{
    public EmployeePhoneNumberListRequestDtoValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId.HasValue).WithMessage("شناسه کارمند باید بزرگتر از صفر باشد.");
        RuleFor(x => x.Search).MaximumLength(32).WithMessage("عبارت جستجو حداکثر ۳۲ کاراکتر است.");
        RuleFor(x => x.SortBy).Must(x => string.IsNullOrWhiteSpace(x) || new[] { "id", "phonenumber", "createddatetime", "modifieddatetime" }.Contains(x.ToLowerInvariant())).WithMessage("ستون مرتب‌سازی شماره‌ها معتبر نیست.");
    }
}
public sealed class EmployeePhoneNumberBulkDeleteDtoValidator : AbstractValidator<EmployeePhoneNumberBulkDeleteDto>
{
    public EmployeePhoneNumberBulkDeleteDtoValidator()
    {
        RuleFor(x => x.Items).NotNull().WithMessage("لیست شماره‌ها الزامی است.")
            .Must(x => x is { Count: > 0 and <= 100 }).WithMessage("برای حذف گروهی بین ۱ تا ۱۰۰ مورد ارسال کنید.")
            .Must(x => x is null || x.Where(i => i is not null).Select(i => i.Id).Distinct().Count() == x.Count).WithMessage("شناسه‌ها نباید تکراری باشند.");
        RuleForEach(x => x.Items).NotNull().WithMessage("اطلاعات ردیف الزامی است.").ChildRules(v =>
        {
            v.RuleFor(x => x.Id).GreaterThan(0).WithMessage("شناسه شماره باید بزرگتر از صفر باشد.");
            v.RuleFor(x => x.RowVersion).Must(EmployeePhoneInputRules.ValidVersion).WithMessage("نسخهٔ رکورد معتبر نیست.");
        });
    }
}
