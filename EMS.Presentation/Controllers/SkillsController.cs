using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Skill;
using EMS.Application.Interfaces.Skill;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

public sealed class SkillsController : BaseApiController
{
    private readonly ISkillService _service;

    public SkillsController(ISkillService service)
    {
        _service = service;
    }

    // GET /api/Skills
    [Permission(Permissions.Skills.View)]
    [HttpGet]
    public async Task<
        ActionResult<ApiResponse<IReadOnlyList<SkillListItemDto>>>>
        GetPaged(
            [FromQuery] SkillListRequestDto request,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetPagedAsync(
            request,
            cancellationToken);

        return Paged(result);
    }

    // GET /api/Skills/groups
    [Permission(Permissions.Skills.View)]
    [HttpGet("groups")]
    public async Task<
        ActionResult<ApiResponse<IReadOnlyList<SkillGroupDto>>>>
        GetGrouped(
            [FromQuery] SkillListRequestDto request,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetGroupedAsync(
            request,
            cancellationToken);

        return Paged(result);
    }

    // GET /api/Skills/10
    [Permission(Permissions.Skills.View)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<SkillDetailDto>>>
        GetById(
            long id,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(
            id,
            cancellationToken);

        return Success(result);
    }

    // POST /api/Skills
    [Permission(Permissions.Skills.Create)]
    [HttpPost]
    [ProducesResponseType<ApiResponse<SkillDetailDto>>(
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SkillDetailDto>>>
        Create(
            [FromBody] SkillCreateDto request,
            CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(
            request,
            cancellationToken);

        return CreatedAt(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    // PUT /api/Skills/10
    [Permission(Permissions.Skills.Update)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<SkillDetailDto>>>
        Update(
            long id,
            [FromBody] SkillUpdateDto request,
            CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            cancellationToken);

        return Updated(result);
    }

    // DELETE /api/Skills/10
    [Permission(Permissions.Skills.Delete)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>>
        Delete(
            long id,
            CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(
            id,
            cancellationToken);

        return Deleted();
    }
}
