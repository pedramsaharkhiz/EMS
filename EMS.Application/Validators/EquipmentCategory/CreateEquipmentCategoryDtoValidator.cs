using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Services.EquipmentCategory;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public sealed class CreateEquipmentCategoryDtoValidator : CatalogCreateValidator<EquipmentCategoryCreateDto>
    {
        public CreateEquipmentCategoryDtoValidator() : base(EquipmentCategoryService.EntityLabel) { }
    }
}
