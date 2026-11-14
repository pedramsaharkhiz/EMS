using EMS.Application.Dtos.Common;
namespace EMS.Application.Dtos.Permission
{
    /// <summary>One permission: <see cref="Name"/> is what the API checks and what is sent when a role is saved; the rest is for people.</summary>
    public class PermissionItemDto
    {
        public string Name { get; set; } = string.Empty;
        /// <summary>The part after the dot (View, Create, Update, Delete, ...).</summary>
        public string Action { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class PermissionGroupDto<TPermission> where TPermission : PermissionItemDto
    {
        /// <summary>The resource: the part of the permission names before the dot.</summary>
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<TPermission> Permissions { get; set; } = new();
    }

    /// <summary>A group of the catalog of all permissions.</summary>
    public sealed class PermissionGroupDto : PermissionGroupDto<PermissionItemDto> { }

    public sealed class RolePermissionItemDto : PermissionItemDto
    {
        /// <summary>Does the role hold this permission?</summary>
        public bool Granted { get; set; }
    }

    public sealed class RolePermissionGroupDto : PermissionGroupDto<RolePermissionItemDto> { }

    /// <summary>Every permission of the system with a mark on those the role holds: what the "role access" screen draws.</summary>
    public sealed class RolePermissionsDto
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        /// <summary>The administrator role: it always holds every permission and cannot be edited.</summary>
        public bool IsSystem { get; set; }
        public int GrantedCount { get; set; }
        public int TotalCount { get; set; }
        public List<RolePermissionGroupDto> Groups { get; set; } = new();
    }

    /// <summary>Replaces the role's permissions with exactly this list (send the whole list, not only the changes).</summary>
    public sealed class SetRolePermissionsDto
    {
        public List<string> Permissions { get; set; } = new();
    }

    /// <summary>The signed-in user as the server sees him now: who he is and what he may do (read fresh from the database, not from the token).</summary>
    public sealed class CurrentUserDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public long? PostId { get; set; }
        public string? PostCode { get; set; }
        public string? PostTitle { get; set; }
        /// <summary>The supporting departments the user works for (empty when none).</summary>
        public List<PersonDepartmentDto> SupportingDepartments { get; set; } = [];
        public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Enumerable.Empty<string>();
    }
}
