using EMS.Application.Dtos.FailureType;

namespace EMS.Application.Interfaces.FailureType
{
    public interface IFailureTypeService
        : ICatalogService<FailureTypeCreateDto, FailureTypeUpdateDto, FailureTypeDetailDto, FailureTypeListItemDto, FailureTypeListRequestDto>
    {
    }
}
