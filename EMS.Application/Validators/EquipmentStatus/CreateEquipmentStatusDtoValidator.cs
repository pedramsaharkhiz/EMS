using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Services.EquipmentStatus;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentStatus
{
    public sealed class CreateEquipmentStatusDtoValidator : CatalogCreateValidator<EquipmentStatusCreateDto>
    {
        public CreateEquipmentStatusDtoValidator() : base(EquipmentStatusService.EntityLabel, x => x.Code) { }
    }
}
