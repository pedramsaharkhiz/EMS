using EMS.Application.Dtos.Table;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.Table
{
    public class TableValidator : AbstractValidator<TableDto> 
    { 
        public TableValidator() 
        { 
           
            //RuleFor(x => x.IsAttachmentEnabled).NotNull();
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'Table' الزامی است.");;

            RuleFor(x => x.TableName).NotEmpty().WithMessage("نام جدول 'Table' الزامی است.");
            RuleFor(x => x.TableName).MaximumLength(100).WithMessage("نام جدول 'Table' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.DisplayName).NotEmpty().WithMessage("نام نمایش 'Table' الزامی است.");
            RuleFor(x => x.DisplayName).MaximumLength(100).WithMessage("نام نمایش 'Table' باید حداکثر 100 کاراکتر باشد.");
        } 
    }
}
