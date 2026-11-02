using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Services.EquipmentType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentType
{
    public sealed class CreateEquipmentTypeDtoValidator : CatalogCreateValidator<EquipmentTypeCreateDto>
    {
        public CreateEquipmentTypeDtoValidator() : base(EquipmentTypeService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.EquipmentCategoryId).GreaterThan(0).WithMessage("شناسه دسته‌بندی تجهیزات باید بزرگتر از صفر باشد.");
        }
    }
}
