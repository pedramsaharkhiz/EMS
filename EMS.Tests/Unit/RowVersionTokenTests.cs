using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;

namespace EMS.Tests.Unit
{
    public class RowVersionTokenTests
    {
        [Fact]
        public void A_record_that_was_never_modified_has_the_token_zero_and_round_trips_to_null()
        {
            Assert.Equal("0", RowVersionToken.From(null));
            Assert.True(RowVersionToken.TryParse("0", out var expected));
            Assert.Null(expected);
        }

        [Fact]
        public void The_token_round_trips_to_the_exact_timestamp()
        {
            var modified = new DateTime(2026, 11, 2, 18, 38, 9, 978, DateTimeKind.Utc).AddTicks(5107);
            Assert.True(RowVersionToken.TryParse(RowVersionToken.From(modified), out var expected));
            Assert.Equal(modified, expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void No_token_means_no_concurrency_check(string? token)
        {
            Assert.False(RowVersionToken.TryParse(token, out var expected));
            Assert.Null(expected);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("-5")]
        [InlineData("1.5")]
        [InlineData("99999999999999999999999")]
        [InlineData("9999999999999999999")]      // larger than DateTime.MaxValue.Ticks
        public void A_malformed_token_is_a_bad_request(string token)
        {
            Assert.Throws<BadRequestException>(() => RowVersionToken.TryParse(token, out _));
        }
    }
}
