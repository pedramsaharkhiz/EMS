using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentUpdateDto : ICatalogUpdateRequest
    {
        public long? CityId { get; set; }
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
        /// <summary>
        /// Optional: branches to put under this department, in the same save (all or nothing). They are ADDED to the current ones (use DELETE .../organizations to take branches off).
        /// Branches of other departments refuse the request unless <c>moveFromOtherDepartments</c> is true. Leave it out for no change.
        /// </summary>
        public SupportingDepartmentOrganizationsDto? Organizations { get; set; }
    }
}