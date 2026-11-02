using EMS.Application.Dtos.RefreshToken;
using FluentValidation;

namespace EMS.Application.Validators.RefreshToken
{
    public class RegisterUserRequestDtoValidator : AbstractValidator<RegisterUserRequestDto>
    {
        public RegisterUserRequestDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage("'کاربر' - نام کاربری الزامی است.").MaximumLength(100).WithMessage("'کاربر' - نام کاربری نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("'کاربر' - کلمه عبور الزامی است.").MinimumLength(6).WithMessage("'کاربر' - کلمه عبور باید حداقل 6 کاراکتر باشد.");
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("'کاربر' - نام الزامی است.").MaximumLength(100).WithMessage("'کاربر' - نام نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("'کاربر' - نام خانوادگی الزامی است.").MaximumLength(100).WithMessage("'کاربر' - نام خانوادگی نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("'کاربر' - ایمیل معتبر نیست.");
        }
    }
    
}
