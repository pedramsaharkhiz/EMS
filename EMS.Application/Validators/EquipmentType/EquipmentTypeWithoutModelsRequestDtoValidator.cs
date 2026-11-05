using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentType
{
    public sealed class EquipmentTypeWithoutModelsRequestDtoValidator : CatalogListRequestValidator<EquipmentTypeWithoutModelsRequestDto>
    {
        public EquipmentTypeWithoutModelsRequestDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.EquipmentCategoryId).GreaterThan(0).When(x => x.EquipmentCategoryId.HasValue).WithMessage("شناسه دسته‌بندی تجهیزات باید بزرگتر از صفر باشد.");
        }
    }
}
