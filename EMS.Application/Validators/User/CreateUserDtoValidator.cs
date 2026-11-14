using EMS.Application.Dtos.User;
using FluentValidation;

namespace EMS.Application.Validators.User;

public sealed class CreateUserDtoValidator
    : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleForEach(x => x.RoleIds)
            .GreaterThan(0).WithMessage("شناسه‌ی نقش باید بزرگ‌تر از صفر باشد.");

        RuleForEach(x => x.SkillIds)
            .GreaterThan(0).WithMessage("شناسه‌ی مهارت باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.RoleIds)
            .Must(x => x == null || x.Count <= 1)
            .WithMessage("هر کاربر فقط یک نقش می‌تواند داشته باشد.");

        RuleFor(x => x.SkillIds)
            .Must(x =>
                x == null ||
                (x.Count <= 100 && x.Distinct().Count() == x.Count))
            .WithMessage("حداکثر ۱۰۰ مهارت بدون تکرار مجاز است.");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("نام کاربری الزامی است.")
            .MaximumLength(20).WithMessage("نام کاربری نمی‌تواند بیشتر از ۲۰ کاراکتر باشد.");

                RuleFor(x => x.Password)
            .NotEmpty().WithMessage("کلمه عبور الزامی است.")
            .MinimumLength(6).WithMessage("کلمه عبور باید حداقل ۶ کاراکتر باشد.")
            .Matches("[A-Z]")
            .WithMessage("کلمه عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد.")
            .Matches("[a-z]")
            .WithMessage("کلمه عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد.")
            .Matches("[0-9]")
            .WithMessage("کلمه عبور باید حداقل یک عدد داشته باشد.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("نام الزامی است.")
            .MaximumLength(100).WithMessage("نام نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است.")
            .MaximumLength(100).WithMessage("نام خانوادگی نمی‌تواند بیشتر از 100 کاراکتر باشد.");

        this.AddIdentityRules(x => x.PersonnelCode, x => x.NationalCode);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("شماره تلفن نمی‌تواند بیشتر از ۲۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("نشانی ایمیل معتبر نیست.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .MaximumLength(256).WithMessage("نشانی ایمیل نمی‌تواند بیشتر از ۲۵۶ کاراکتر باشد.");

        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("شناسه‌ی سمت باید بزرگ‌تر از صفر باشد.")
            .When(x => x.PostId.HasValue);

        RuleFor(x => x.SupportingDepartmentIds)
            .Must(ids => ids is null || ids.Count <= 50).WithMessage("یک کاربر نمی‌تواند بیشتر از ۵۰ واحد پشتیبانی داشته باشد.");
        RuleForEach(x => x.SupportingDepartmentIds)
            .GreaterThan(0).WithMessage("شناسه‌ی واحد پشتیبانی باید بزرگ‌تر از صفر باشد.");
    }
}
