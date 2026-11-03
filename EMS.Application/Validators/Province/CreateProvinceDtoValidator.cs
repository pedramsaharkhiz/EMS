using EMS.Application.Dtos.Province;
using EMS.Application.Services.Province;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Province
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreateProvinceDtoValidator : CatalogCreateValidator<ProvinceCreateDto>
    {
        public CreateProvinceDtoValidator() : base(ProvinceService.EntityLabel, x => x.Code) { }
    }
}
