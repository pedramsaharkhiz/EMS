using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using Serilog.Context;

namespace EMS.Presentation.Middlewares
{
    /// <summary>
    /// Runs after authentication and adds UserId / UserName to every log event emitted
    /// while the rest of the request is processed (services, repositories, EF, ...).
    /// </summary>
    public class UserLogContextMiddleware
    {
        private readonly RequestDelegate _next;

        public UserLogContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                await _next(context);
                return;
            }

            using (LogContext.PushProperty("UserId", currentUser.UserId))
            using (LogContext.PushProperty("UserName", LogSanitizer.Sanitize(currentUser.UserName)))
            {
                await _next(context);
            }
        }
    }
}
