using EMS.Application.Dtos.Role;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.Role
{
    public class RoleValidator : AbstractValidator<RoleDto>
    {
        public RoleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("نام 'نقش' الزامی است.");
            RuleFor(x => x.Name).MaximumLength(256).WithMessage("نام 'نقش' باید حداکثر 256 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نقش' الزامی است.");;
        }
    }
}
