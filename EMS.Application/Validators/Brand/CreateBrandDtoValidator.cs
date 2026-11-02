using EMS.Application.Dtos.Brand;
using EMS.Application.Dtos.EquipmentCategory;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class CreateBrandDtoValidator : AbstractValidator<BrandCreateDto>
    {
        public CreateBrandDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان برند الزامی است.").MaximumLength(100).WithMessage("عنوان برند نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان برند نمی‌تواند فقط شامل فاصله باشد.");
        }
    }
}
