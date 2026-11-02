using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Presentation.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EMS.Presentation.Middlewares
{
    /// <summary>
    /// Converts exceptions into the API's error responses (see <see cref="ApiProblem"/>) and logs them at a level that
    /// matches their meaning: expected client errors are not noisy, security-relevant ones go to the security
    /// category, unexpected failures are logged as errors with the full exception.
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

                var errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                await ApiProblem.WriteAsync(context, ApiProblem.Validation(context, errors));
            }
            catch (UnauthorizedAccessException ex)
            {
                _securityLogger.LogWarning(
                    "Unauthorized request {Method} {Path} from {ClientIp}: {Reason}",
                    context.Request.Method, context.Request.Path.Value,
                    context.Connection.RemoteIpAddress?.ToString(), LogSanitizer.Sanitize(ex.Message, 200));

                var code = (ex as UnauthorizedAppException)?.Code ?? ErrorCodes.Unauthorized;
                await ApiProblem.WriteAsync(context, StatusCodes.Status401Unauthorized, code, ex.Message);
            }
            catch (NotFoundException ex)
            {
                LogClientError(context, ex);
                await ApiProblem.WriteAsync(context, StatusCodes.Status404NotFound, ex.Code, ex.Message);
            }
            catch (ConflictException ex)
            {
                LogClientError(context, ex);
                await ApiProblem.WriteAsync(context, StatusCodes.Status409Conflict, ex.Code, ex.Message);
            }
            catch (BadRequestException ex)
            {
                LogClientError(context, ex);
                await ApiProblem.WriteAsync(context, StatusCodes.Status400BadRequest, ex.Code, ex.Message);
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

                // Never leak internal details: a fixed message plus the correlation id to quote to support.
                await ApiProblem.WriteAsync(context, StatusCodes.Status500InternalServerError,
                    ErrorCodes.InternalError, "خطای داخلی سرور رخ داده است.");
            }
        }

        private void LogClientError(HttpContext context, Exception ex)
        {
            _logger.LogInformation(
                "{ExceptionType} for {Method} {Path}: {Message}",
                ex.GetType().Name, context.Request.Method, context.Request.Path.Value,
                LogSanitizer.Sanitize(ex.Message, 300));
        }
    }
}
