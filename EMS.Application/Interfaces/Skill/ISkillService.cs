using EMS.Application.Common.Models;
using EMS.Application.Dtos.Skill;
using EMS.Application.Interfaces;

namespace EMS.Application.Interfaces.Skill;

public interface ISkillService : ICatalogService<
    SkillCreateDto,
    SkillUpdateDto,
    SkillDetailDto,
    SkillListItemDto,
    SkillListRequestDto>
{
    Task<PagedResult<SkillGroupDto>> GetGroupedAsync(
        SkillListRequestDto request,
        CancellationToken cancellationToken = default);
}
