using EMS.Application.Dtos.ActionEntity;

namespace EMS.Application.Interfaces.ActionEntity
{
    public interface IActionEntityService
        : ICatalogService<ActionEntityCreateDto, ActionEntityUpdateDto, ActionEntityDetailDto, ActionEntityListItemDto, ActionEntityListRequestDto>
    {
    }
}
