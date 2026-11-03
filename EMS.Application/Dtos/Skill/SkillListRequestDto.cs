using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Skill;

public sealed class SkillListRequestDto
    : PagedRequest, ICatalogListRequest
{
    public string? Search { get; set; }

    public long? Id { get; set; }

    public string? Title { get; set; }

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public bool IncludeInactive { get; set; }

    public long? TechnicianUserId { get; set; }

    public bool? HasTechnician { get; set; }

    public int? MinTechnicianCount { get; set; }

    public int? MaxTechnicianCount { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    public DateTime? ModifiedFrom { get; set; }

    public DateTime? ModifiedTo { get; set; }

    // isActive | code | hasTechnician
    public string? GroupBy { get; set; }
}
