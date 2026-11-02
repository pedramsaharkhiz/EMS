using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Services.EquipmentLocationType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentLocationType
{
    public sealed class EquipmentLocationTypeListRequestDtoValidator : CatalogListRequestValidator<EquipmentLocationTypeListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
