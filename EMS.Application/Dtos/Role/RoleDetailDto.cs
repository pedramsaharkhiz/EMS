namespace EMS.Application.Dtos.Role;

public sealed class RoleDetailDto : RoleListItemDto
{
    public DateTime CreatedDateTime { get; set; }

    public DateTime? ModifiedDateTime { get; set; }
}