using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Services.EquipmentStatus;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentStatus
{
    public sealed class EquipmentStatusListRequestDtoValidator : CatalogListRequestValidator<EquipmentStatusListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
