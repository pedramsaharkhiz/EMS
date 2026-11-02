using EMS.Application.Dtos.User;
using FluentValidation;

namespace EMS.Application.Validators.User;

public sealed class SetUserPasswordDtoValidator
    : AbstractValidator<SetUserPasswordDto>
{
    public SetUserPasswordDtoValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(6)
            .Matches("[A-Z]")
            .WithMessage("کلمه عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد.")
            .Matches("[a-z]")
            .WithMessage("کلمه عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد.")
            .Matches("[0-9]")
            .WithMessage("کلمه عبور باید حداقل یک عدد داشته باشد.");
    }
}