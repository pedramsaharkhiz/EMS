using Microsoft.AspNetCore.Authorization;

namespace EMS.Presentation.Security
{
    /// <summary>
    /// Marks a read-only reference-data endpoint (brands, provinces, statuses, ...) that any signed-in user may call,
    /// because forms need these lists as dropdowns whatever the user's role is. It only requires a valid access token
    /// (still checked on every request: active user, current security stamp). Writing the same data keeps its own
    /// [Permission]. Use it on GET actions only; a unit test enforces that.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LookupAttribute : AuthorizeAttribute
    {
    }
}
