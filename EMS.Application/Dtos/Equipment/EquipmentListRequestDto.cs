using EMS.Application.Common.Models;
using EMS.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// The equipment list and the Excel export take the same filters (the export ignores paging). Id filters accept several
    /// values: <c>?statusId=1&amp;statusId=2</c>. Text filters contain-match. Everything is optional and combined with AND.
    /// Sort with <c>sortBy</c>: owner, yaasCode, serialNumber, assetNumber, category, type, model, brand, status, locationKind,
    /// owner, organization, department, city, province, technician, createdDateTime (default: id).
    /// </summary>
    public class EquipmentListRequestDto : PagedRequest
    {
        /// <summary>Looks in the Yaas code, serial number and asset number.</summary>
        public string? Search { get; set; }
        public string? YaasCode { get; set; }
        public string? SerialNumber { get; set; }
        public string? AssetNumber { get; set; }

        [FromQuery(Name = "owner")] public EquipmentOwner[]? Owners { get; set; }
        [FromQuery(Name = "statusId")] public long[]? StatusIds { get; set; }
        [FromQuery(Name = "categoryId")] public long[]? CategoryIds { get; set; }
        /// <summary>Matches the effective type (the equipment's own type, or the type of its model).</summary>
        [FromQuery(Name = "typeId")] public long[]? TypeIds { get; set; }
        [FromQuery(Name = "modelId")] public long[]? ModelIds { get; set; }
        [FromQuery(Name = "brandId")] public long[]? BrandIds { get; set; }
        /// <summary>1 = with a branch, 2 = with a supporting department.</summary>
        [FromQuery(Name = "locationKind")] public EquipmentLocationKind[]? LocationKinds { get; set; }
        [FromQuery(Name = "organizationId")] public long[]? OrganizationIds { get; set; }
        [FromQuery(Name = "fieldId")] public long[]? FieldIds { get; set; }
        [FromQuery(Name = "organizationManagementId")] public long[]? OrganizationManagementIds { get; set; }
        [FromQuery(Name = "zoneId")] public long[]? ZoneIds { get; set; }
        /// <summary>The department that serves the equipment's branch (the response field <c>supportingDepartment...</c>).</summary>
        [FromQuery(Name = "supportingDepartmentId")] public long[]? SupportingDepartmentIds { get; set; }
        /// <summary>The department the equipment itself is at (the response field <c>currentSupportingDepartment...</c>).</summary>
        [FromQuery(Name = "currentSupportingDepartmentId")] public long[]? CurrentSupportingDepartmentIds { get; set; }
        [FromQuery(Name = "cityId")] public long[]? CityIds { get; set; }
        [FromQuery(Name = "provinceId")] public long[]? ProvinceIds { get; set; }
        [FromQuery(Name = "technicianUserId")] public long[]? TechnicianUserIds { get; set; }

        /// <summary>Created at or after (UTC, ISO 8601).</summary>
        public DateTime? CreatedFrom { get; set; }
        /// <summary>Created at or before (UTC, ISO 8601), inclusive as sent.</summary>
        public DateTime? CreatedTo { get; set; }

        /// <summary>null = active equipment only (default); true/false = only active / only deactivated.</summary>
        public bool? IsActive { get; set; }
    }
}
