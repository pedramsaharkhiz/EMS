using EMS.Domain;
using FluentValidation;
using System.Linq.Expressions;

namespace EMS.Application.Validators.User;

public static class UserIdentityValidation
{
    public static void AddIdentityRules<T>(this AbstractValidator<T> validator,
        Expression<Func<T, string>> personnel, Expression<Func<T, string?>> national)
    {
        validator.RuleFor(personnel).Cascade(CascadeMode.Stop)
            .Must(x => UserIdentityRules.Normalize(x).Length > 0).WithMessage("کد پرسنلی الزامی است.")
            .Must(x => UserIdentityRules.Normalize(x).Length <= 32).WithMessage("کد پرسنلی نمی‌تواند بیشتر از ۳۲ کاراکتر باشد.");
        validator.RuleFor(national).Cascade(CascadeMode.Stop)
            .Must(x => UserIdentityRules.NormalizeOptional(x) is null || UserIdentityRules.HasNationalFormat(x))
            .WithMessage("کد ملی باید دقیقاً ۱۰ رقم باشد.")
            .Must(x => UserIdentityRules.NormalizeOptional(x) is null || UserIdentityRules.IsValidNationalCode(x))
            .WithMessage("کد ملی واردشده معتبر نیست؛ لطفاً آن را بررسی کنید.");
    }
}
