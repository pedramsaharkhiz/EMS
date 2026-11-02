using EMS.Application.Dtos.RefreshToken;
using FluentValidation;

namespace EMS.Application.Validators.RefreshToken
{
    public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenRequestDtoValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("'ورود' - رفرش نوکن الزامی است.");
        }
    }
    
}
