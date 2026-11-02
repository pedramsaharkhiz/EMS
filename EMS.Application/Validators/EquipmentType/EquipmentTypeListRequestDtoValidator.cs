using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Services.EquipmentType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentType
{
    public sealed class EquipmentTypeListRequestDtoValidator : CatalogListRequestValidator<EquipmentTypeListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
