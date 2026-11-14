using EMS.Application.Dtos.Common;
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
        /// <summary>Where he works: every supporting department he belongs to, each with its city and province (empty when none).</summary>
        public List<PersonDepartmentDto> SupportingDepartments { get; set; } = [];
    }
}
