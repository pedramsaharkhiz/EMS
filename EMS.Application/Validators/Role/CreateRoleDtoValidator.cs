using EMS.Application.Dtos.Role;
using FluentValidation;

namespace EMS.Application.Validators.Role;

public sealed class CreateRoleDtoValidator
    : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("نام نقش الزامی است.")
            .MaximumLength(256)
            .WithMessage("نام نقش نمی‌تواند بیشتر از ۲۵۶ کاراکتر باشد.");
    }
}