using EMS.Application.Dtos.Brand;
using EMS.Application.Dtos.EquipmentCategory;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class UpdateBrandDtoValidator : AbstractValidator<BrandUpdateDto>
    {
        public UpdateBrandDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان برند نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("شناسه  'برند' باید بزرگتر از صفر باشد.");

        }
    }
}
