using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Services.EquipmentModel;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentModel
{
    public sealed class EquipmentModelListRequestDtoValidator : CatalogListRequestValidator<EquipmentModelListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
