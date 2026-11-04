using EMS.Application.Common.Globalization;
using System.Globalization;

namespace EMS.Tests.Unit
{
    public class PersianDateTests
    {
        // Nowruz and leap-year boundaries of the official Iranian calendar (the .NET calendar is arithmetic, so these pin it).
        [Theory]
        [InlineData("2021-03-20", "1399/12/30")]   // 1399 is a leap year
        [InlineData("2021-03-21", "1400/01/01")]
        [InlineData("2022-03-21", "1401/01/01")]
        [InlineData("2023-03-21", "1402/01/01")]
        [InlineData("2024-03-20", "1403/01/01")]
        [InlineData("2025-03-20", "1403/12/30")]   // 1403 is a leap year
        [InlineData("2025-03-21", "1404/01/01")]
        [InlineData("2025-09-23", "1404/07/01")]   // 1 Mehr: six months of 31 days after Nowruz
        [InlineData("2025-12-22", "1404/10/01")]
        [InlineData("2026-03-21", "1405/01/01")]
        public void Calendar_dates_convert_to_the_official_jalali_date(string gregorian, string jalali)
        {
            // invariant parsing: the machine culture may itself be Persian, which would read "2021" as a Jalali year
            var date = DateOnly.ParseExact(gregorian, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.Equal(jalali, PersianDate.ToDate(date));
        }

        [Fact]
        public void A_utc_moment_is_shown_in_tehran_time_across_midnight()
        {
            // 21:00 UTC on 20 March is 00:30 on 21 March in Tehran (UTC+03:30): already the next day, already Nowruz.
            var utc = new DateTime(2025, 3, 20, 21, 0, 0, DateTimeKind.Utc);
            Assert.Equal("1404/01/01", PersianDate.ToDate(utc));
            Assert.Equal("1404/01/01 00:30", PersianDate.ToDateTime(utc));

            // and 20:00 UTC is still the last day of 1403 (23:30 in Tehran)
            Assert.Equal("1403/12/30 23:30", PersianDate.ToDateTime(new DateTime(2025, 3, 20, 20, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void An_unspecified_kind_is_read_as_utc_like_the_database_values()
        {
            var unspecified = new DateTime(2025, 12, 31, 21, 0, 0, DateTimeKind.Unspecified);
            var utc = DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
            Assert.Equal(PersianDate.ToDateTime(utc), PersianDate.ToDateTime(unspecified));
            Assert.Equal("1404/10/11 00:30", PersianDate.ToDateTime(unspecified));   // 1 Jan 2026, 00:30 in Tehran
        }

        [Fact]
        public void A_local_kind_is_converted_through_utc()
        {
            var local = new DateTime(2025, 3, 21, 12, 0, 0, DateTimeKind.Utc).ToLocalTime();
            Assert.Equal("1404/01/01 15:30", PersianDate.ToDateTime(local));
        }

        [Fact]
        public void Offsets_and_nullable_values_are_handled()
        {
            Assert.Equal("1404/01/01 00:30", PersianDate.ToDateTime(new DateTimeOffset(2025, 3, 21, 0, 30, 0, TimeSpan.FromHours(3.5))));
            Assert.Equal(string.Empty, PersianDate.ToDate((DateTime?)null));
            Assert.Equal(string.Empty, PersianDate.ToDateTime((DateTime?)null));
            Assert.Equal("1404/01/01", PersianDate.ToDate((DateTime?)new DateTime(2025, 3, 21, 9, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void Text_is_zero_padded_so_it_sorts_like_the_dates()
        {
            var early = PersianDate.ToDateTime(new DateTime(2025, 4, 1, 1, 2, 0, DateTimeKind.Utc));       // 1404/01/12
            var later = PersianDate.ToDateTime(new DateTime(2025, 7, 1, 1, 2, 0, DateTimeKind.Utc));        // 1404/04/10
            Assert.Matches(@"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}$", early);
            Assert.True(string.CompareOrdinal(early, later) < 0);
        }

        [Fact]
        public void A_date_outside_the_persian_calendar_falls_back_to_iso_instead_of_throwing()
        {
            Assert.Equal("0001-01-01", PersianDate.ToDate(DateOnly.MinValue));
            // year 1: ISO text (the offset is the historical local mean time of that era, so only the date part is asserted)
            Assert.StartsWith("0001-01-01 ", PersianDate.ToDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)));
        }
    }
}
