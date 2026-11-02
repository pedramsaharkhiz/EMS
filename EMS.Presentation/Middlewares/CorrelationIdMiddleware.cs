using Serilog.Context;

namespace EMS.Presentation.Middlewares
{
    /// <summary>
    /// Assigns every request a correlation id (taken from the incoming X-Correlation-ID header when it
    /// is well-formed, otherwise generated), exposes it as <see cref="HttpContext.TraceIdentifier"/>,
    /// returns it in the response header, and attaches it to every log event of the request.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string LogPropertyName = "CorrelationId";

        private const int MinLength = 8;
        private const int MaxLength = 64;

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
            var correlationId = IsValid(incoming) ? incoming! : Guid.NewGuid().ToString("N");

            context.TraceIdentifier = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty(LogPropertyName, correlationId))
            {
                await _next(context);
            }
        }

        /// <summary>
        /// Accepts only short, header-safe identifiers so a client can't inject control characters
        /// or oversized values into logs.
        /// </summary>
        public static bool IsValid(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < MinLength || value.Length > MaxLength)
                return false;

            foreach (var c in value)
            {
                var ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                         || c == '-' || c == '_' || c == '.' || c == ':';
                if (!ok)
                    return false;
            }

            return true;
        }
    }
}
