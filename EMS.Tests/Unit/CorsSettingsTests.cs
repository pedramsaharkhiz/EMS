using EMS.Presentation.Security;

namespace EMS.Tests.Unit
{
    public class CorsSettingsTests
    {
        private static string? Validate(params string[] origins) => new CorsSettings { AllowedOrigins = origins.ToList() }.Validate();

        [Theory]
        [InlineData("http://192.168.200.62")]
        [InlineData("https://192.168.200.62")]
        [InlineData("http://localhost:5173")]
        [InlineData("https://app.example.com")]
        [InlineData("http://192.168.200.62:8080")]
        public void A_scheme_host_and_optional_port_is_valid(string origin) => Assert.Null(Validate(origin));

        [Fact]
        public void An_empty_list_is_valid_and_allows_nothing() => Assert.Null(Validate());

        [Theory]
        [InlineData("*")]
        [InlineData("192.168.200.62")]
        [InlineData("http://192.168.200.62/")]
        [InlineData("http://192.168.200.62/app")]
        [InlineData("http://192.168.200.62:80")]
        [InlineData("https://192.168.200.62:443")]
        [InlineData("ftp://192.168.200.62")]
        [InlineData("")]
        public void Anything_else_is_refused_at_start_up(string origin) => Assert.NotNull(Validate(origin));
    }
}
