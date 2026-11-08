using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>The allowed browser origins come from configuration (Cors:AllowedOrigins), compared exactly.</summary>
    public class CorsTests : ApiTestBase
    {
        public CorsTests(EmsApiFactory factory) : base(factory) { }

        private async Task<HttpResponseMessage> PreflightAsync(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/Identity/Login");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
            return await _client.SendAsync(request);
        }

        [Theory]
        [InlineData("http://localhost:3000")]
        [InlineData("http://192.168.200.62")]
        public async Task A_configured_origin_passes_the_preflight_and_may_send_credentials(string origin)
        {
            var response = await PreflightAsync(origin);

            Assert.True(response.IsSuccessStatusCode, $"preflight -> {(int)response.StatusCode}");
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        }

        [Theory]
        [InlineData("http://192.168.200.62:80")]    // the browser never sends the default port, so this is a different string
        [InlineData("https://192.168.200.62")]      // another scheme is another origin
        [InlineData("http://192.168.200.63")]
        [InlineData("http://evil.example")]
        public async Task An_origin_that_is_not_configured_gets_no_cors_headers(string origin)
        {
            var response = await PreflightAsync(origin);

            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"), $"{origin} must not be allowed");
        }

        [Fact]
        public async Task A_normal_request_from_a_configured_origin_carries_the_allow_origin_header()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Identity/Login")
            {
                Content = JsonContent("{\"userName\":\"nobody\",\"password\":\"Wrong1pass\"}")
            };
            request.Headers.Add("Origin", "http://192.168.200.62");
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("http://192.168.200.62", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        }

        private static StringContent JsonContent(string json) => new(json, System.Text.Encoding.UTF8, "application/json");
    }
}
