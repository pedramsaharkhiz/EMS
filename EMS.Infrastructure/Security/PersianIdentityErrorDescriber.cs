using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace EMS.Infrastructure.Security
{
    /// <summary>
    /// ASP.NET Identity reports failures in English. Its descriptions are passed to the client as the error message, so they
    /// are replaced with Persian text. The error <c>Code</c>s are unchanged (the services compare them, e.g. ConcurrencyFailure).
    /// </summary>
    public sealed class PersianIdentityErrorDescriber : IdentityErrorDescriber
    {
        private static IdentityError E(string code, string description) => new() { Code = code, Description = description };

        public override IdentityError DefaultError() => E(nameof(DefaultError), "خطای ناشناخته‌ای رخ داد.");
        public override IdentityError ConcurrencyFailure() => E(nameof(ConcurrencyFailure), "این رکورد در این فاصله توسط کاربر دیگری تغییر کرده است. اطلاعات را دوباره بارگذاری کنید و تغییرات را مجدداً اعمال کنید.");
        public override IdentityError PasswordMismatch() => E(nameof(PasswordMismatch), "کلمه عبور نادرست است.");
        public override IdentityError InvalidToken() => E(nameof(InvalidToken), "توکن نامعتبر است.");
        public override IdentityError RecoveryCodeRedemptionFailed() => E(nameof(RecoveryCodeRedemptionFailed), "استفاده از کد بازیابی ناموفق بود.");
        public override IdentityError LoginAlreadyAssociated() => E(nameof(LoginAlreadyAssociated), "این ورود پیش‌تر به کاربر دیگری متصل شده است.");
        public override IdentityError InvalidUserName(string? userName) => E(nameof(InvalidUserName), $"نام کاربری «{userName}» معتبر نیست؛ فقط حروف انگلیسی، عدد و نشانه‌های - . _ @ + مجاز است.");
        public override IdentityError InvalidEmail(string? email) => E(nameof(InvalidEmail), $"نشانی ایمیل «{email}» معتبر نیست.");
        public override IdentityError DuplicateUserName(string userName) => E(nameof(DuplicateUserName), $"نام کاربری «{userName}» قبلاً ثبت شده است.");
        public override IdentityError DuplicateEmail(string email) => E(nameof(DuplicateEmail), $"نشانی ایمیل «{email}» قبلاً ثبت شده است.");
        public override IdentityError InvalidRoleName(string? role) => E(nameof(InvalidRoleName), $"نام نقش «{role}» معتبر نیست.");
        public override IdentityError DuplicateRoleName(string role) => E(nameof(DuplicateRoleName), $"نقش «{role}» قبلاً ثبت شده است.");
        public override IdentityError UserAlreadyHasPassword() => E(nameof(UserAlreadyHasPassword), "برای این کاربر کلمه عبور از قبل تعیین شده است.");
        public override IdentityError UserLockoutNotEnabled() => E(nameof(UserLockoutNotEnabled), "قفل شدن حساب برای این کاربر فعال نیست.");
        public override IdentityError UserAlreadyInRole(string role) => E(nameof(UserAlreadyInRole), $"کاربر از قبل نقش «{role}» را دارد.");
        public override IdentityError UserNotInRole(string role) => E(nameof(UserNotInRole), $"کاربر نقش «{role}» را ندارد.");
        public override IdentityError PasswordTooShort(int length) => E(nameof(PasswordTooShort), $"کلمه عبور باید حداقل {length} کاراکتر باشد.");
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => E(nameof(PasswordRequiresUniqueChars), $"کلمه عبور باید حداقل {uniqueChars} کاراکتر متفاوت داشته باشد.");
        public override IdentityError PasswordRequiresNonAlphanumeric() => E(nameof(PasswordRequiresNonAlphanumeric), "کلمه عبور باید حداقل یک نشانه (مثل ! یا @) داشته باشد.");
        public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit), "کلمه عبور باید حداقل یک عدد داشته باشد.");
        public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower), "کلمه عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد.");
        public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper), "کلمه عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد.");
    }
}
