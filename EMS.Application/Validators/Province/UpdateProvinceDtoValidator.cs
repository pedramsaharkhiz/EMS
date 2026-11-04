using EMS.Application.Dtos.Province;
using EMS.Application.Services.Province;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Province
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdateProvinceDtoValidator : CatalogUpdateValidator<ProvinceUpdateDto>
    {
        public UpdateProvinceDtoValidator() : base(ProvinceService.EntityLabel, x => x.Code) { }
    }
}
