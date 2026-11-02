using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Services.FailureCategory;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.FailureCategory
{
    public sealed class FailureCategoryListRequestDtoValidator : CatalogListRequestValidator<FailureCategoryListRequestDto>
    {
        // paging and sort-direction rules come from the base class
    }
}
