namespace EMS.Application.Dtos.Role;

public sealed class UpdateRoleDto
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
    public string? RowVersion { get; set; }
}