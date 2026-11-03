using EMS.Application.Dtos.RefreshToken;
using FluentValidation;

namespace EMS.Application.Validators.RefreshToken
{
    public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenRequestDtoValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("رفرش توکن الزامی است.");
        }
    }
    
}
