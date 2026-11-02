using EMS.Application.Common.Exceptions;
using EMS.Presentation.Middlewares;
using EMS.Tests.Support;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EMS.Tests.Unit
{
    public class GlobalExceptionMiddlewareTests
    {
        public static IEnumerable<object[]> ClientErrors() => new[]
        {
            new object[] { new NotFoundException("not here"), 404, LogLevel.Information, "EMS.Presentation.Middlewares.GlobalExceptionMiddleware", "not_found" },
            new object[] { new NotFoundException("no such brand", ErrorCodes.ParentNotFound), 404, LogLevel.Information, "EMS.Presentation.Middlewares.GlobalExceptionMiddleware", "parent_not_found" },
            new object[] { new ConflictException("duplicate"), 409, LogLevel.Information, "EMS.Presentation.Middlewares.GlobalExceptionMiddleware", "conflict" },
            new object[] { new ConflictException("duplicate title", ErrorCodes.DuplicateTitle), 409, LogLevel.Information, "EMS.Presentation.Middlewares.GlobalExceptionMiddleware", "duplicate_title" },
            new object[] { new BadRequestException("bad"), 400, LogLevel.Information, "EMS.Presentation.Middlewares.GlobalExceptionMiddleware", "bad_request" },
            new object[] { new UnauthorizedAccessException("denied"), 401, LogLevel.Warning, "EMS.Security", "unauthorized" },
            new object[] { new UnauthorizedAppException("bad login", ErrorCodes.InvalidCredentials), 401, LogLevel.Warning, "EMS.Security", "invalid_credentials" },
        };

        [Theory]
        [MemberData(nameof(ClientErrors))]
        public async Task Maps_expected_errors_keeping_the_response_contract_and_logs_at_the_right_level(
            Exception exception, int expectedStatus, LogLevel expectedLevel, string expectedCategory, string expectedCode)
        {
            var (context, logs) = await RunAsync(exception);

            Assert.Equal(expectedStatus, context.Response.StatusCode);
            Assert.Equal("application/problem+json", context.Response.ContentType);
            var body = await ReadBodyAsync(context);
            Assert.Equal(exception.Message, body.GetProperty("message").GetString());
            Assert.Equal(exception.Message, body.GetProperty("detail").GetString());   // RFC 7807 member = legacy member
            Assert.Equal("trace-12345678", body.GetProperty("correlationId").GetString());
            Assert.Equal(expectedStatus, body.GetProperty("status").GetInt32());
            Assert.Equal(expectedCode, body.GetProperty("code").GetString());
            Assert.StartsWith("https://", body.GetProperty("type").GetString());
            Assert.Equal("/api/sample", body.GetProperty("instance").GetString());

            var entry = Assert.Single(logs.Entries);
            Assert.Equal(expectedLevel, entry.Level);
            Assert.Equal(expectedCategory, entry.Category);
            Assert.Null(entry.Exception); // expected errors do not need a stack trace in the log
        }

        [Fact]
        public async Task Validation_errors_return_400_with_the_error_list_and_are_logged_without_values()
        {
            var failures = new[] { new ValidationFailure("UserName", "required"), new ValidationFailure("Password", "too short") };
            var (context, logs) = await RunAsync(new ValidationException(failures));

            Assert.Equal(400, context.Response.StatusCode);
            var body = await ReadBodyAsync(context);
            Assert.Equal("validation_failed", body.GetProperty("code").GetString());
            Assert.Equal("One or more validation errors occurred.", body.GetProperty("title").GetString());
            Assert.Equal("اطلاعات ارسالی نامعتبر است.", body.GetProperty("message").GetString());
            var errors = body.GetProperty("errors");
            Assert.Equal("required", errors.GetProperty("UserName")[0].GetString());   // field -> messages (same shape MVC uses)
            Assert.Equal("too short", errors.GetProperty("Password")[0].GetString());

            var entry = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("2 error(s)", entry.Message);
        }

        [Fact]
        public async Task Unexpected_exceptions_return_500_with_correlation_id_and_log_the_full_exception()
        {
            var boom = new InvalidOperationException("secret internal detail");
            var (context, logs) = await RunAsync(boom);

            Assert.Equal(500, context.Response.StatusCode);
            var body = await ReadBodyAsync(context);
            Assert.Equal("trace-12345678", body.GetProperty("correlationId").GetString());
            Assert.Equal("internal_error", body.GetProperty("code").GetString());
            Assert.DoesNotContain("secret internal detail", body.GetProperty("message").GetString());
            Assert.DoesNotContain("secret internal detail", body.GetProperty("detail").GetString());

            var entry = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(boom, entry.Exception);
            Assert.Contains("trace-12345678", entry.Message);
        }

        [Fact]
        public async Task Database_update_failures_are_logged_as_errors_and_return_500()
        {
            var (context, logs) = await RunAsync(new DbUpdateException("fk violation", new Exception("inner")));

            Assert.Equal(500, context.Response.StatusCode);
            var entry = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Contains("Database update failed", entry.Message);
        }

        [Fact]
        public async Task A_client_abort_is_not_reported_as_a_server_error()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var (context, logs) = await RunAsync(new OperationCanceledException(cts.Token), requestAborted: cts.Token);

            Assert.Equal(499, context.Response.StatusCode);
            var entry = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
        }

        [Fact]
        public async Task A_cancellation_that_is_not_a_client_abort_is_still_an_error()
        {
            var (context, logs) = await RunAsync(new OperationCanceledException("internal timeout"));

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal(LogLevel.Error, Assert.Single(logs.Entries).Level);
        }

        [Fact]
        public async Task Log_forging_attempts_in_exception_messages_are_neutralised()
        {
            var (_, logs) = await RunAsync(new NotFoundException("x\r\n[ERR] forged line"));

            var entry = Assert.Single(logs.Entries);
            Assert.DoesNotContain('\n', entry.Message);
            Assert.DoesNotContain('\r', entry.Message);
        }

        private static async Task<(DefaultHttpContext Context, ListLoggerFactory Logs)> RunAsync(
            Exception exception, CancellationToken requestAborted = default)
        {
            var logs = new ListLoggerFactory();
            var context = new DefaultHttpContext { TraceIdentifier = "trace-12345678" };
            context.Request.Path = "/api/sample";
            context.Response.Body = new MemoryStream();
            context.RequestAborted = requestAborted;

            var middleware = new GlobalExceptionMiddleware(
                _ => throw exception,
                logs.CreateLogger<GlobalExceptionMiddleware>(),
                logs);

            await middleware.InvokeAsync(context);
            return (context, logs);
        }

        private static async Task<JsonElement> ReadBodyAsync(DefaultHttpContext context)
        {
            context.Response.Body.Position = 0;
            using var doc = await JsonDocument.ParseAsync(context.Response.Body);
            return doc.RootElement.Clone();
        }
    }
}
