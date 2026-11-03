using EMS.Application.Dtos.User;
using FluentValidation;

namespace EMS.Application.Validators.User;

public sealed class CreateUserDtoValidator
    : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleForEach(x => x.RoleIds)
            .GreaterThan(0);

        RuleForEach(x => x.SkillIds)
            .GreaterThan(0);

        RuleFor(x => x.RoleIds)
            .Must(x =>
                x == null ||
                (x.Count <= 100 && x.Distinct().Count() == x.Count))
            .WithMessage("حداکثر ۱۰۰ نقش بدون تکرار مجاز است.");

        RuleFor(x => x.SkillIds)
            .Must(x =>
                x == null ||
                (x.Count <= 100 && x.Distinct().Count() == x.Count))
            .WithMessage("حداکثر ۱۰۰ مهارت بدون تکرار مجاز است.");

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[A-Z]")
            .WithMessage("کلمه عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد.")
            .Matches("[a-z]")
            .WithMessage("کلمه عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد.")
            .Matches("[0-9]")
            .WithMessage("کلمه عبور باید حداقل یک عدد داشته باشد.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PersonnelCode)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.NationalCode)
            .MaximumLength(10)
            .When(x => !string.IsNullOrWhiteSpace(x.NationalCode));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .MaximumLength(256);

        RuleFor(x => x.SupportingDepartmentId)
            .GreaterThan(0)
            .When(x => x.SupportingDepartmentId.HasValue);
    }
}
