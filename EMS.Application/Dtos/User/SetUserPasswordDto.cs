namespace EMS.Application.Dtos.User;

public sealed class SetUserPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}