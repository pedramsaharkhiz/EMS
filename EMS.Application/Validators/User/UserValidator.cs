using EMS.Application.Dtos.User;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.User
{
    public class UserValidator : AbstractValidator<UserDto>
    {
        public UserValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage("نام کاربری 'کاربر' الزامی است.");
            RuleFor(x => x.UserName).MaximumLength(20).WithMessage("نام کاربری 'کاربر' باید حداکثر 20 کاراکتر باشد.");
            //RuleFor(x => x.NormalizedUserName).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.NormalizedUserName)).WithMessage("نام کاربری نرمال شده 'کاربر' باید حداکثر 20 کاراکتر باشد."); 
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("نام 'کاربر' الزامی است.");
            RuleFor(x => x.FirstName).MaximumLength(100).WithMessage("نام 'کاربر' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("نام خانوادگی 'کاربر' الزامی است.");
            RuleFor(x => x.LastName).MaximumLength(100).WithMessage("نام خانوادگی 'کاربر' باید حداکثر 100 کاراکتر باشد.");
            this.AddIdentityRules(x => x.PersonnelCode, x => x.NationalCode);
            RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber)).WithMessage("شماره تلفن 'کاربر' باید حداکثر 20 کاراکتر باشد.");
            RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email)).WithMessage("ایمیل 'کاربر' باید حداکثر 256 کاراکتر باشد.");
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email)).WithMessage("ایمیل 'کاربر' معتبر نیست.");
            RuleForEach(x => x.SupportingDepartmentIds).GreaterThan(0).WithMessage("شناسه 'دپارتمان  پشتیبانی' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'کاربر' الزامی است.");;
        }
    }
}