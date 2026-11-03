using EMS.Application.Dtos.UserRole;
using FluentValidation;

namespace EMS.Application.Validators.UserRole;

public sealed class SetRoleUsersDtoValidator
    : AbstractValidator<SetRoleUsersDto>
{
    public SetRoleUsersDtoValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull()
            .WithMessage("فهرست شناسه‌های کاربران الزامی است.");

        RuleForEach(x => x.UserIds)
            .GreaterThan(0)
            .WithMessage("شناسه‌ی کاربر باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.UserIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("شناسه‌ی تکراری کاربر مجاز نیست.");
    }
}