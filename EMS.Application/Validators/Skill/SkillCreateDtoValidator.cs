using EMS.Application.Dtos.Skill;
using FluentValidation;

namespace EMS.Application.Validators.Skill;

public sealed class SkillCreateDtoValidator
    : AbstractValidator<SkillCreateDto>
{
    public SkillCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("عنوان مهارت الزامی است.")
            .MaximumLength(100)
            .WithMessage("عنوان مهارت حداکثر ۱۰۰ کاراکتر است.");

        RuleFor(x => x.Code)
            .MaximumLength(20)
            .WithMessage("کد مهارت حداکثر ۲۰ کاراکتر است.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("توضیحات مهارت حداکثر ۵۰۰ کاراکتر است.");
    }
}
