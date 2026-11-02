using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Services.FailureCategory;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.FailureCategory
{
    public sealed class CreateFailureCategoryDtoValidator : CatalogCreateValidator<FailureCategoryCreateDto>
    {
        public CreateFailureCategoryDtoValidator() : base(FailureCategoryService.EntityLabel) { }
    }
}
