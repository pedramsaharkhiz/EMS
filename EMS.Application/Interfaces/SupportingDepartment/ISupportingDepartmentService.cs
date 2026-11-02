using EMS.Application.Dtos.SupportingDepartment;

namespace EMS.Application.Interfaces.SupportingDepartment
{
    /// <summary>CRUD contract of the catalog "دپارتمان پشتیبانی" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface ISupportingDepartmentService
        : ICatalogService<SupportingDepartmentCreateDto, SupportingDepartmentUpdateDto, SupportingDepartmentDetailDto, SupportingDepartmentListItemDto, SupportingDepartmentListRequestDto>
    {
    }
}
