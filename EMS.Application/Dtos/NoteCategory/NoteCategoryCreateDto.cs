using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.NoteCategory
{
    public class NoteCategoryCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
    }
}
