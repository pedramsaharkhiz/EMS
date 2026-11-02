using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Services.EquipmentCategory;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public sealed class EquipmentCategoryListRequestDtoValidator : CatalogListRequestValidator<EquipmentCategoryListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
