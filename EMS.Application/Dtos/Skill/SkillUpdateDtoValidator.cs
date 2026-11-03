using EMS.Application.Common.Models;
using EMS.Application.Dtos.Skill;
using FluentValidation;

namespace EMS.Application.Validators.Skill;

public sealed class SkillUpdateDtoValidator
    : AbstractValidator<SkillUpdateDto>
{
    public SkillUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.Title is not null)
            .WithMessage("عنوان مهارت باید غیرخالی و حداکثر ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.Code)
            .MaximumLength(20)
            .WithMessage("کد مهارت حداکثر ۲۰ کاراکتر است.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("توضیحات مهارت حداکثر ۵۰۰ کاراکتر است.");

        RuleFor(x => x.RowVersion)
            .Must(IsValidRowVersion)
            .WithMessage("مقدار RowVersion نامعتبر است.");
    }

    private static bool IsValidRowVersion(string? value)
    {
        try
        {
            RowVersionToken.TryParse(value, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
