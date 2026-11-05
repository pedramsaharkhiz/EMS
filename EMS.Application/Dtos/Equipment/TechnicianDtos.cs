using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Equipment
{
    public class TechnicianListRequestDto : PagedRequest
    {
        /// <summary>Looks in the name and the personnel code.</summary>
        public string? Search { get; set; }
    }

    public class TechnicianItemDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        // where the technician works: his supporting department and its city and province
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? CityId { get; set; }
        public string? CityTitle { get; set; }
        public long? ProvinceId { get; set; }
        public string? ProvinceTitle { get; set; }
    }
}
