namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentListItemDto
    {
        public long Id { get; set; }
        public long CityId { get; set; }
        public string CityTitle { get; set; } = string.Empty;
        public long ProvinceId { get; set; }
        public string ProvinceTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        /// <summary>How many active branches this department serves (see POST /api/SupportingDepartment/{id}/organizations).</summary>
        public int OrganizationCount { get; set; }
    }
}
