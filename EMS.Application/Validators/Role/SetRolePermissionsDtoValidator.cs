using EMS.Application.Common.Security;
using EMS.Application.Dtos.Permission;
using FluentValidation;

namespace EMS.Application.Validators.Role
{
    public sealed class SetRolePermissionsDtoValidator : AbstractValidator<SetRolePermissionsDto>
    {
        public SetRolePermissionsDtoValidator()
        {
            RuleFor(x => x.Permissions).NotNull().WithMessage("فهرست دسترسی‌ها الزامی است.");
            RuleForEach(x => x.Permissions)
                .Must(p => Permissions.All.Contains(p) || Permissions.Retired.Contains(p))
                .WithMessage("دسترسی «{PropertyValue}» وجود ندارد.");
        }
    }
}
