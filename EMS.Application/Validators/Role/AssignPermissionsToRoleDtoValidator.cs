using EMS.Application.Common.Security;
using EMS.Application.Dtos.Role;
using FluentValidation;

namespace EMS.Application.Validators.Role
{
    public sealed class AssignPermissionsToRoleDtoValidator : AbstractValidator<AssignPermissionsToRoleDto>
    {
        public AssignPermissionsToRoleDtoValidator()
        {
            RuleFor(x => x.RoleName).NotEmpty().WithMessage("نام نقش الزامی است.");
            RuleFor(x => x.Permissions).NotNull().WithMessage("فهرست دسترسی‌ها الزامی است.");
            RuleForEach(x => x.Permissions)
                .Must(p => Permissions.All.Contains(p) || Permissions.Retired.Contains(p))
                .WithMessage("دسترسی «{PropertyValue}» وجود ندارد.");
        }
    }
}
