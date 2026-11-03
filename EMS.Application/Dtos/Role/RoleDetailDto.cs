namespace EMS.Application.Dtos.Role;

public sealed class RoleDetailDto : RoleListItemDto
{
    public DateTime CreatedDateTime { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
    public string RowVersion { get; set; } = string.Empty;
}