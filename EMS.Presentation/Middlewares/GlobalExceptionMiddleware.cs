using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace EMS.Presentation.Middlewares
{
    /// <summary>
    /// Converts exceptions into the API's JSON error responses and logs them at a level that matches
    /// their meaning: expected client errors are not noisy, security-relevant ones go to the security
    /// category, unexpected failures are logged as errors with the full exception.
    /// The response bodies keep their historical shape; <c>correlationId</c> is an additive field.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private const int ClientClosedRequestStatusCode = 499;

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly ILogger _securityLogger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = logger;
            _securityLogger = loggerFactory.CreateLogger(LogCategories.Security);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client went away; nothing is wrong on the server side.
                _logger.LogInformation(
                    "Request {Method} {Path} was cancelled by the client",
                    context.Request.Method, context.Request.Path.Value);

                if (!context.Response.HasStarted)
                    context.Response.StatusCode = ClientClosedRequestStatusCode;
            }
            catch (ValidationException ex)
            {
                _logger.LogInformation(
                    "Validation failed for {Method} {Path}: {ErrorCount} error(s) on {Properties}",
                    context.Request.Method, context.Request.Path.Value,
                    ex.Errors.Count(), ex.Errors.Select(e => e.PropertyName).Distinct().ToArray());

                var errors = ex.Errors.Select(x => new { x.PropertyName, x.ErrorMessage });
                await WriteAsync(context, HttpStatusCode.BadRequest,
                    new { message = "Validation failed.", errors, correlationId = context.TraceIdentifier });
            }
            catch (UnauthorizedAccessException ex)
            {
                _securityLogger.LogWarning(
                    "Unauthorized request {Method} {Path} from {ClientIp}: {Reason}",
                    context.Request.Method, context.Request.Path.Value,
                    context.Connection.RemoteIpAddress?.ToString(), LogSanitizer.Sanitize(ex.Message, 200));

                await WriteAsync(context, HttpStatusCode.Unauthorized,
                    new { message = ex.Message, correlationId = context.TraceIdentifier });
            }
            catch (NotFoundException ex)
            {
                LogClientError(context, ex);
                await WriteAsync(context, HttpStatusCode.NotFound,
                    new { message = ex.Message, correlationId = context.TraceIdentifier });
            }
            catch (ConflictException ex)
            {
                LogClientError(context, ex);
                await WriteAsync(context, HttpStatusCode.Conflict,
                    new { message = ex.Message, correlationId = context.TraceIdentifier });
            }
            catch (BadRequestException ex)
            {
                LogClientError(context, ex);
                await WriteAsync(context, HttpStatusCode.BadRequest,
                    new { message = ex.Message, correlationId = context.TraceIdentifier });
            }
            catch (Exception ex)
            {
                var correlationId = context.TraceIdentifier;

                if (ex is DbUpdateException)
                {
                    _logger.LogError(ex,
                        "Database update failed for {Method} {Path}. CorrelationId: {CorrelationId}",
                        context.Request.Method, context.Request.Path.Value, correlationId);
                }
                else
                {
                    _logger.LogError(ex,
                        "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}",
                        context.Request.Method, context.Request.Path.Value, correlationId);
                }

                await WriteAsync(context, HttpStatusCode.InternalServerError,
                    new { message = "خطای داخلی سرور رخ داده است.", correlationId });
            }
        }

        private void LogClientError(HttpContext context, Exception ex)
        {
            _logger.LogInformation(
                "{ExceptionType} for {Method} {Path}: {Message}",
                ex.GetType().Name, context.Request.Method, context.Request.Path.Value,
                LogSanitizer.Sanitize(ex.Message, 300));
        }

        private static async Task WriteAsync(HttpContext context, HttpStatusCode statusCode, object body)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}
