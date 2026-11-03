using EMS.Application.Dtos.TechnicianCategory;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.TechnicianCategory
{
    public class TechnicianCategoryValidator : AbstractValidator<TechnicianCategoryDto> 
    {
        public TechnicianCategoryValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'نوع تکنسین' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'نوع تکنسین' باید حداکثر 100 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نوع تکنسین' الزامی است.");;
        }
    }
}
