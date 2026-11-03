using EMS.Application.Dtos.UserRole;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.UserRole
{
    public class UserRoleValidator : AbstractValidator<UserRoleDto>
    {
        public UserRoleValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0).WithMessage("UserId 'UserRole' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("RoleId 'UserRole' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.IsActive).NotNull();
        }
    }
}
