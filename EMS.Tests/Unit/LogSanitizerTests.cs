using EMS.Application.Common.Logging;

namespace EMS.Tests.Unit
{
    public class LogSanitizerTests
    {
        [Fact]
        public void Replaces_control_characters_so_values_cannot_forge_log_lines()
        {
            var result = LogSanitizer.Sanitize("admin\r\n2026-01-01 INFO fake entry\t");

            Assert.DoesNotContain('\r', result!);
            Assert.DoesNotContain('\n', result!);
            Assert.DoesNotContain('\t', result!);
            Assert.StartsWith("admin", result);
        }

        [Fact]
        public void Truncates_long_values()
        {
            var result = LogSanitizer.Sanitize(new string('a', 500), 50);

            Assert.Equal(51, result!.Length); // 50 chars + ellipsis
            Assert.EndsWith("…", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Returns_null_and_empty_unchanged(string? value)
        {
            Assert.Equal(value, LogSanitizer.Sanitize(value));
        }

        [Fact]
        public void Leaves_normal_values_untouched()
        {
            Assert.Equal("پدرام-saharkhiz_01", LogSanitizer.Sanitize("پدرام-saharkhiz_01"));
        }
    }
}
