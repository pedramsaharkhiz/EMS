using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.ActionEntity;
using FluentValidation;

namespace EMS.Application.Validators.ActionEntity
{


    public class ActionEntityValidator : AbstractValidator<ActionEntityDto>
    {
        public ActionEntityValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'اکشن' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'اکشن' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'اکشن' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'اکشن' باید حداکثر 500 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'اکشن' الزامی است.");
        }
    }
}
