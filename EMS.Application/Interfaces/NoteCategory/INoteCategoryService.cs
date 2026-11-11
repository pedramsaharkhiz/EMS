using EMS.Application.Dtos.NoteCategory;

namespace EMS.Application.Interfaces.NoteCategory
{
    public interface INoteCategoryService
        : ICatalogService<NoteCategoryCreateDto, NoteCategoryUpdateDto, NoteCategoryDetailDto, NoteCategoryListItemDto, NoteCategoryListRequestDto>
    {
    }
}
