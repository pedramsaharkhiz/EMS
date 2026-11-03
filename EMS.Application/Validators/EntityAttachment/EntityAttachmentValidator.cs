using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.EntityAttachment;
using FluentValidation;

namespace EMS.Application.Validators.EntityAttachment
{


    public class EntityAttachmentValidator : AbstractValidator<EntityAttachmentDto>
    {
        public EntityAttachmentValidator()
        {
            RuleFor(x => x.TableId).GreaterThan(0).WithMessage("شناسه 'TableId' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EntityId).GreaterThan(0).WithMessage("شناسه 'EntityId' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.StreamId).NotEmpty().When(x => x.StreamId.HasValue);
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'پیوست' باید حداکثر 500 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'پیوست' الزامی است.");
        }
    }
}
