using Microsoft.AspNetCore.Authorization;

namespace EMS.Presentation.Security
{
    /// <summary>
    /// For endpoints that serve many entities through one route (attachments): the caller must be signed in, and the action itself
    /// authorizes with the permission of the entity the request is about (e.g. <c>Equipments.View</c> for an equipment's files), which is
    /// only known once the table name or the attachment is resolved. Every such action must call the check before doing anything; the
    /// attachment integration tests cover each endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EntityPermissionAttribute : AuthorizeAttribute
    {
    }
}
