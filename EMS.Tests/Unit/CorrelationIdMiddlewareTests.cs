using EMS.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;

namespace EMS.Tests.Unit
{
    public class CorrelationIdMiddlewareTests
    {
        [Theory]
        [InlineData("abcd1234", true)]
        [InlineData("0HN3F1-ABC.def_ghi:01", true)]
        [InlineData("short", false)]                       // too short
        [InlineData("has space in it", false)]
        [InlineData("line\r\nbreak-injection", false)]
        [InlineData("<script>alert(1)</script>", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void IsValid_accepts_only_short_header_safe_identifiers(string? value, bool expected)
        {
            Assert.Equal(expected, CorrelationIdMiddleware.IsValid(value));
        }

        [Fact]
        public void IsValid_rejects_oversized_values()
        {
            Assert.False(CorrelationIdMiddleware.IsValid(new string('a', 65)));
            Assert.True(CorrelationIdMiddleware.IsValid(new string('a', 64)));
        }

        [Fact]
        public async Task Generates_an_id_when_the_request_has_none()
        {
            var seenTraceId = await RunAsync(null);

            Assert.True(CorrelationIdMiddleware.IsValid(seenTraceId));
        }

        [Fact]
        public async Task Reuses_a_valid_incoming_id()
        {
            var seenTraceId = await RunAsync("client-supplied-id-001");

            Assert.Equal("client-supplied-id-001", seenTraceId);
        }

        [Fact]
        public async Task Replaces_an_invalid_incoming_id()
        {
            var seenTraceId = await RunAsync("bad id with spaces");

            Assert.NotEqual("bad id with spaces", seenTraceId);
            Assert.True(CorrelationIdMiddleware.IsValid(seenTraceId));
        }

        // The response header is verified end-to-end in the integration tests
        // (DefaultHttpContext does not execute OnStarting callbacks).
        private static async Task<string> RunAsync(string? incoming)
        {
            var context = new DefaultHttpContext();
            if (incoming is not null)
                context.Request.Headers[CorrelationIdMiddleware.HeaderName] = incoming;

            var seen = string.Empty;
            var middleware = new CorrelationIdMiddleware(ctx =>
            {
                seen = ctx.TraceIdentifier;
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);
            return seen;
        }
    }
}
