using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using EMS.Application.Common.Exceptions;

namespace EMS.Presentation.Errors
{
    /// <summary>
    /// The one place that builds error responses. Every error of the API (business rules, validation, authentication,
    /// authorization, unknown routes, unexpected failures) is an RFC 9457/7807 "problem details" document
    /// (<c>application/problem+json</c>):
    /// <code>
    /// { "type", "title", "status", "detail", "instance",     // RFC members
    ///   "code",          // stable machine-readable error code - branch on this
    ///   "message",       // same text as detail (kept for older clients)
    ///   "correlationId", "traceId",
    ///   "errors" }       // validation only: { "Field": ["message", ...] }
    /// </code>
    /// </summary>
    public static class ApiProblem
    {
        public const string ContentType = "application/problem+json";
        public const string ValidationTitle = "One or more validation errors occurred.";
        public const string ValidationMessage = "اطلاعات ارسالی نامعتبر است.";

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static ProblemDetails Create(
            HttpContext context,
            int status,
            string code,
            string message,
            IDictionary<string, string[]>? errors = null,
            string? title = null)
        {
            var problem = new ProblemDetails
            {
                Type = TypeFor(status),
                Title = title ?? ReasonPhrases.GetReasonPhrase(status),
                Status = status,
                Detail = message,
                Instance = context.Request.Path.Value
            };

            problem.Extensions["code"] = code;
            problem.Extensions["message"] = message;
            problem.Extensions["correlationId"] = context.TraceIdentifier;
            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (errors is { Count: > 0 })
                problem.Extensions["errors"] = errors;

            return problem;
        }

        public static ProblemDetails Validation(HttpContext context, IDictionary<string, string[]> errors) =>
            Create(context, StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, ValidationMessage, errors, ValidationTitle);

        /// <summary>Problem for a response that has a status code but no body (401/403/404/405 from the framework, ...).</summary>
        public static ProblemDetails ForStatus(HttpContext context, int status)
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

            return Create(context, status, code, message);
        }

        public static async Task WriteAsync(HttpContext context, ProblemDetails problem)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = ContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, Json));
        }

        public static Task WriteAsync(HttpContext context, int status, string code, string message, IDictionary<string, string[]>? errors = null) =>
            WriteAsync(context, Create(context, status, code, message, errors));

        // RFC 9110 section links, one per status
        private static string TypeFor(int status) => status switch
        {
            400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            405 => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
            409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            >= 500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            _ => "about:blank"
        };
    }

    /// <summary>Lets MVC (model-validation failures) return the same problem document.</summary>
    public sealed class ApiProblemResult : IActionResult
    {
        private readonly ProblemDetails _problem;

        public ApiProblemResult(ProblemDetails problem) => _problem = problem;

        public Task ExecuteResultAsync(ActionContext context) => ApiProblem.WriteAsync(context.HttpContext, _problem);
    }
}
