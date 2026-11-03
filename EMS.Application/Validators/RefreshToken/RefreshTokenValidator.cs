using EMS.Application.Dtos.RefreshToken;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.RefreshToken
{
    public class RefreshTokenValidator : AbstractValidator<RefreshTokenDto>
    {
        public RefreshTokenValidator()
        {
            //RuleFor(x => x.UserId).GreaterThan(0);
            //RuleFor(x => x.TokenHash).NotEmpty().MaximumLength(128);
            //RuleFor(x => x.CreatedByIp).NotEmpty().MaximumLength(45);
            //RuleFor(x => x.ExpiresDateTime).NotEmpty();
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'رفرش توکن' الزامی است.");;
        }
    }
    
}
