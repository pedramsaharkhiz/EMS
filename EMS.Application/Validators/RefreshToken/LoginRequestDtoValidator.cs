using EMS.Application.Dtos.RefreshToken;
using FluentValidation;

namespace EMS.Application.Validators.RefreshToken
{
    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage("'ورود' - نام کاربری الزامی است.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("'ورود' - کلمه عبور الزامی است.");
        }
    }
    
}
