namespace EMS.Application.Dtos.User;

public sealed class UserDetailDto : UserListItemDto
{
    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
    public string RowVersion { get; set; } = string.Empty;
}