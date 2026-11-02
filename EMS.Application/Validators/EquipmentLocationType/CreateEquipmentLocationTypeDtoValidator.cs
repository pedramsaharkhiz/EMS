using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Services.EquipmentLocationType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentLocationType
{
    public sealed class CreateEquipmentLocationTypeDtoValidator : CatalogCreateValidator<EquipmentLocationTypeCreateDto>
    {
        public CreateEquipmentLocationTypeDtoValidator() : base(EquipmentLocationTypeService.EntityLabel, x => x.Code) { }
    }
}
