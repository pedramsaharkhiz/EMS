using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Services.EquipmentCategory;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public sealed class UpdateEquipmentCategoryDtoValidator : CatalogUpdateValidator<EquipmentCategoryUpdateDto>
    {
        public UpdateEquipmentCategoryDtoValidator() : base(EquipmentCategoryService.EntityLabel) { }
    }
}
