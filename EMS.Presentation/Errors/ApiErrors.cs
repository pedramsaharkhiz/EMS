using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EMS.Presentation.Errors
{
    /// <summary>
    /// The one place that builds error responses. Every error of the API (business rules, validation, authentication,
    /// authorization, unknown routes, unexpected failures) is the same <see cref="ApiResponse{T}"/> envelope as a success,
    /// with <c>success = false</c>, a user-facing <c>message</c> and an <c>error</c> holding the stable <c>code</c> and the
    /// field-level <c>details</c>. The HTTP status code carries the kind of failure (400, 401, 403, 404, 409, 500, ...).
    /// </summary>
    public static class ApiErrors
    {
        public const string ContentType = "application/json";
        public const string ValidationMessage = "اطلاعات ارسالی نامعتبر است.";

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static ApiResponse<object> Create(
            HttpContext context,
            string code,
            string message,
            IEnumerable<ApiErrorDetail>? details = null) =>
            ApiResponse.Fail(context.TraceIdentifier, code, message, details);

        /// <summary>Field-level errors (<c>property name -&gt; messages</c>) as a 400 body.</summary>
        public static ApiResponse<object> Validation(HttpContext context, IDictionary<string, string[]> errors) =>
            Create(context, ErrorCodes.ValidationFailed, ValidationMessage,
                errors.SelectMany(e => e.Value.Select(message => new ApiErrorDetail { Field = FieldName(e.Key), Message = message })));

        /// <summary>The error for a response that has a status code but no body (401/403/404/405 from the framework, ...).</summary>
        public static ApiResponse<object> ForStatus(HttpContext context, int status)
        {
            var (code, message) = status switch
            {
                StatusCodes.Status400BadRequest => (ErrorCodes.BadRequest, "درخواست نامعتبر است."),
                StatusCodes.Status401Unauthorized => (ErrorCodes.Unauthorized, "احراز هویت انجام نشده یا توکن نامعتبر است."),
                StatusCodes.Status403Forbidden => (ErrorCodes.Forbidden, "شما به این عملیات دسترسی ندارید."),
                StatusCodes.Status404NotFound => (ErrorCodes.NotFound, "مسیر یا منبع درخواستی یافت نشد."),
                StatusCodes.Status405MethodNotAllowed => (ErrorCodes.MethodNotAllowed, "این متد برای مسیر درخواستی مجاز نیست."),
                StatusCodes.Status409Conflict => (ErrorCodes.Conflict, "درخواست با وضعیت فعلی منبع در تعارض است."),
                >= 500 => (ErrorCodes.InternalError, "خطای داخلی سرور رخ داده است."),
                _ => ($"http_{status}", "درخواست قابل پردازش نیست.")
            };

            return Create(context, code, message);
        }

        public static async Task WriteAsync(HttpContext context, int status, ApiResponse<object> body)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = status;
            context.Response.ContentType = ContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, Json));
        }

        public static Task WriteAsync(HttpContext context, int status, string code, string message) =>
            WriteAsync(context, status, Create(context, code, message));

        /// <summary>
        /// A request field as the client spelled it in the JSON body: camelCase per segment, no "$." prefix
        /// ("PageSize" -> "pageSize", "$.title" -> "title", "UserIds[0]" -> "userIds[0]", "Items[0].Name" -> "items[0].name").
        /// </summary>
        public static string FieldName(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "$")
                return string.Empty;
            if (key.StartsWith("$.", StringComparison.Ordinal))
                key = key[2..];

            return string.Join('.', key.Split('.').Select(segment =>
                segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));
        }
    }

    /// <summary>Lets MVC (model-validation failures) return the same error document.</summary>
    public sealed class ApiErrorResult : IActionResult
    {
        private readonly int _status;
        private readonly ApiResponse<object> _body;

        public ApiErrorResult(int status, ApiResponse<object> body)
        {
            _status = status;
            _body = body;
        }

        public Task ExecuteResultAsync(ActionContext context) => ApiErrors.WriteAsync(context.HttpContext, _status, _body);
    }
}
