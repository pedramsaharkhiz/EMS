using EMS.Application.Dtos.EquipmentCategory;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class CreateEquipmentCategoryDtoValidator : AbstractValidator<EquipmentCategoryCreateDto>
    {
        public CreateEquipmentCategoryDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان دسته‌بندی الزامی است.").MaximumLength(100).WithMessage("عنوان دسته‌بندی نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان دسته‌بندی نمی‌تواند فقط شامل فاصله باشد.");
        }
    }
}
