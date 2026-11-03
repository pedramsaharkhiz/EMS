using EMS.Application.Dtos.UserRole;
using FluentValidation;

namespace EMS.Application.Validators.UserRole;

public sealed class SetUserRolesDtoValidator
    : AbstractValidator<SetUserRolesDto>
{
    public SetUserRolesDtoValidator()
    {
        RuleFor(x => x.RoleIds)
            .NotNull()
            .WithMessage("فهرست شناسه‌های نقش الزامی است.");

        RuleForEach(x => x.RoleIds)
            .GreaterThan(0)
            .WithMessage("شناسه‌ی نقش باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.RoleIds)
            .Must(ids => ids == null || ids.Count <= 1)
            .WithMessage("هر کاربر فقط یک نقش می‌تواند داشته باشد.");

        RuleFor(x => x.RoleIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("شناسه‌ی تکراری نقش مجاز نیست.");
    }
}