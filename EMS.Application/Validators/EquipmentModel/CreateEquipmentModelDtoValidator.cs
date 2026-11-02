using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Services.EquipmentModel;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentModel
{
    public sealed class CreateEquipmentModelDtoValidator : CatalogCreateValidator<EquipmentModelCreateDto>
    {
        public CreateEquipmentModelDtoValidator() : base(EquipmentModelService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.EquipmentTypeId).GreaterThan(0).WithMessage("شناسه نوع تجهیزات باید بزرگتر از صفر باشد.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("شناسه برند باید بزرگتر از صفر باشد.");
        }
    }
}
