using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Services.EquipmentLocationType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentLocationType
{
    public sealed class UpdateEquipmentLocationTypeDtoValidator : CatalogUpdateValidator<EquipmentLocationTypeUpdateDto>
    {
        public UpdateEquipmentLocationTypeDtoValidator() : base(EquipmentLocationTypeService.EntityLabel, x => x.EquipmentLocationTypeId, x => x.Code) { }
    }
}
