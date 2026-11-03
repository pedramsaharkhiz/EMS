using Serilog.Events;
using System.Net;

namespace EMS.Tests.Integration
{
    public class RefreshTokenRaceTests : ApiTestBase
    {
        public RefreshTokenRaceTests(EmsApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Parallel_refreshes_with_the_same_token_produce_exactly_one_winner_and_no_false_theft_alarm()
        {
            var login = await LoginAdminAsync();
            var correlationIds = Enumerable.Range(0, 6).Select(i => NewCorrelationId($"it-race{i}")).ToList();

            var responses = await Task.WhenAll(correlationIds.Select(corr =>
                SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = login.RefreshToken }, corr)));

            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));

            // The losers are reported as concurrent use (warning), never as token reuse/theft (error).
            var events = correlationIds.SelectMany(Sink.ForCorrelation).ToList();
            Assert.DoesNotContain(events, e => Rendered(e).Contains("TokenReuseDetected"));
            Assert.True(events.Count(e => e.Level == LogEventLevel.Warning && Rendered(e).Contains("ConcurrentUse")) >= 1);

            // The token issued to the winner keeps working.
            var winner = responses.Single(r => r.StatusCode == HttpStatusCode.OK);
            var newRefresh = (await ReadDataAsync(winner)).GetProperty("refreshToken").GetString()!;
            Assert.Equal(HttpStatusCode.OK,
                (await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = newRefresh })).StatusCode);
        }

        [Fact]
        public async Task Simultaneous_sign_ins_of_the_same_user_all_succeed()
        {
            var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
                SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = AdminPassword })));

            var errors = string.Join(" || ", Sink.Snapshot()
                .Where(e => e.Level == LogEventLevel.Error && e.Exception is not null)
                .Select(e => e.Exception!.GetType().Name + ": " + e.Exception.Message.Split('\n')[0]));
            Assert.All(responses, r => Assert.True(r.StatusCode == HttpStatusCode.OK, "status " + r.StatusCode + "; server errors: " + errors));
        }
    }
}
