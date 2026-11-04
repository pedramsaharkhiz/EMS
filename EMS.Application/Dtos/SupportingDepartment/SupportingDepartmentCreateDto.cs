using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentCreateDto : ICatalogCreateRequest
    {
        public long CityId { get; set; }
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Optional: branches to put under this department, in the same save (all or nothing). The department can be created with no branches at all.
        /// Branches of other departments refuse the request unless <c>moveFromOtherDepartments</c> is true. Leave it out for no change.
        /// </summary>
        public SupportingDepartmentOrganizationsDto? Organizations { get; set; }
    }
}