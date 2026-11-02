using EMS.Infrastructure.Persistence;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// End-to-end checks: real HTTP requests through the real pipeline, asserting on the log events produced.
    /// </summary>
    public class LoggingIntegrationTests : ApiTestBase
    {
        public LoggingIntegrationTests(EmsApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Failed_login_for_unknown_user_returns_a_generic_401_and_logs_the_real_reason_as_a_security_warning()
        {
            var corr = NewCorrelationId("it-unknown");
            var response = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "nobody-here", password = "Wrong1pass" }, corr);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(corr, response.Headers.GetValues("X-Correlation-ID").Single());

            var json = await ReadJsonAsync(response);
            Assert.Equal(GenericLoginError, json.GetProperty("message").GetString());   // no hint about *why*
            Assert.Equal(corr, json.GetProperty("correlationId").GetString());

            var events = Sink.ForCorrelation(corr);
            Assert.Contains(events, e => e.Level == LogEventLevel.Warning
                                         && CapturingSink.SourceContext(e) == "EMS.Security"
                                         && Rendered(e).Contains("Login failed (UnknownUser)"));
            Assert.Contains(events, e => e.Level == LogEventLevel.Warning
                                         && CapturingSink.PropertyText(e, "StatusCode") == "401"
                                         && CapturingSink.PropertyText(e, "RequestPath") == "/api/Identity/Login");
        }

        [Fact]
        public async Task Successful_login_is_logged_and_no_secret_ever_reaches_the_logs()
        {
            var corr = NewCorrelationId("it-login");
            var (accessToken, refreshToken, _) = await LoginAsync(AdminUserName, AdminPassword, corr);

            Assert.Contains(Sink.ForCorrelation(corr), e => e.Level == LogEventLevel.Information
                                                            && CapturingSink.SourceContext(e) == "EMS.Security"
                                                            && Rendered(e).Contains("Login succeeded"));

            // Exercise more paths that touch credentials, then scan *everything* captured so far.
            await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = "NotThePassword1" }, NewCorrelationId("it-leak"));
            await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken }, NewCorrelationId("it-leak"));

            foreach (var e in Sink.Snapshot())
            {
                var text = Rendered(e) + " " + string.Join(" ", e.Properties.Values.Select(v => v.ToString())) + " " + e.Exception;
                Assert.DoesNotContain(AdminPassword, text);
                Assert.DoesNotContain("NotThePassword1", text);
                Assert.DoesNotContain(accessToken, text);
                Assert.DoesNotContain(refreshToken, text);
            }
        }

        [Fact]
        public async Task Repeated_wrong_passwords_are_logged_and_the_account_lockout_is_visible()
        {
            var admin = await LoginAsync(AdminUserName, AdminPassword);
            await CreateUserAsync(admin.AccessToken, "locktest1", "Passw0rd!x");

            for (var i = 0; i < 5; i++)
            {
                var response = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "locktest1", password = "Wrong1pass" }, NewCorrelationId("it-lock"));
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }

            var security = Sink.Snapshot()
                .Where(e => CapturingSink.SourceContext(e) == "EMS.Security" && Rendered(e).Contains("locktest1"))
                .Select(Rendered).ToList();

            Assert.Equal(4, security.Count(m => m.Contains("Login failed (InvalidPassword)")));
            Assert.Contains(security, m => m.Contains("Login failed (LockedOut)"));

            // Even the correct password is now rejected, and the log says why.
            var corr = NewCorrelationId("it-locked");
            var locked = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "locktest1", password = "Passw0rd!x" }, corr);
            Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corr), e => Rendered(e).Contains("Login failed (LockedOut)"));
        }

        [Fact]
        public async Task Refresh_token_rotation_replay_and_garbage_are_all_distinguishable_in_the_log()
        {
            var login = await LoginAsync(AdminUserName, AdminPassword);

            // 1. valid rotation
            var corrOk = NewCorrelationId("it-refresh-ok");
            var ok = await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = login.RefreshToken }, corrOk);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corrOk), e => Rendered(e).Contains("Refresh token rotated"));

            // 2a. presented again immediately: indistinguishable from a duplicate/concurrent request => warning, not theft
            var corrDup = NewCorrelationId("it-refresh-dup");
            var dup = await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = login.RefreshToken }, corrDup);
            Assert.Equal(HttpStatusCode.Unauthorized, dup.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corrDup), e => e.Level == LogEventLevel.Warning && Rendered(e).Contains("ConcurrentUse"));
            Assert.DoesNotContain(Sink.ForCorrelation(corrDup), e => Rendered(e).Contains("TokenReuseDetected"));

            // 2b. the already-used token is presented again much later => replay
            await AgeRevokedTokensAsync(TimeSpan.FromMinutes(5));
            var corrReplay = NewCorrelationId("it-refresh-replay");
            var replay = await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = login.RefreshToken }, corrReplay);
            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corrReplay), e => e.Level == LogEventLevel.Error
                                                                  && CapturingSink.SourceContext(e) == "EMS.Security"
                                                                  && Rendered(e).Contains("TokenReuseDetected"));

            // 3. a token that never existed
            var corrJunk = NewCorrelationId("it-refresh-junk");
            var junk = await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = "does-not-exist" }, corrJunk);
            Assert.Equal(HttpStatusCode.Unauthorized, junk.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corrJunk), e => e.Level == LogEventLevel.Warning && Rendered(e).Contains("TokenNotFound"));
        }

        private async Task AgeRevokedTokensAsync(TimeSpan age)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            foreach (var token in await db.UserRefreshTokens.Where(x => x.RevokedDateTime != null).ToListAsync())
                token.RevokedDateTime = DateTime.UtcNow - age;
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task A_user_without_the_required_permission_gets_403_and_the_denial_is_logged()
        {
            var admin = await LoginAsync(AdminUserName, AdminPassword);
            await CreateUserAsync(admin.AccessToken, "noperms1", "Passw0rd!x");
            var plain = await LoginAsync("noperms1", "Passw0rd!x");

            var corr = NewCorrelationId("it-403");
            var response = await SendAsync(HttpMethod.Get, "/api/Users", correlationId: corr, bearer: plain.AccessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var events = Sink.ForCorrelation(corr);
            Assert.Contains(events, e => e.Level == LogEventLevel.Warning
                                         && CapturingSink.SourceContext(e) == "EMS.Security"
                                         && Rendered(e).Contains("Access denied")
                                         && CapturingSink.PropertyText(e, "RequiredPermission") == "Users.View"
                                         && CapturingSink.PropertyText(e, "UserId") == plain.UserId.ToString());
            Assert.Contains(events, e => CapturingSink.PropertyText(e, "StatusCode") == "403" && e.Level == LogEventLevel.Warning);
            // The acting user is attached to the request log line.
            Assert.Contains(events, e => CapturingSink.PropertyText(e, "StatusCode") == "403"
                                         && CapturingSink.PropertyText(e, "UserId") == plain.UserId.ToString());
        }

        [Fact]
        public async Task Catalog_endpoints_require_their_own_permission_and_each_denial_is_logged_with_the_permission_name()
        {
            var admin = await LoginAsync(AdminUserName, AdminPassword);
            await CreateUserAsync(admin.AccessToken, "brandless1", "Passw0rd!x");
            var plain = await LoginAsync("brandless1", "Passw0rd!x");

            var corrList = NewCorrelationId("it-brand-list");
            var corrCreate = NewCorrelationId("it-brand-create");
            var list = await SendAsync(HttpMethod.Get, "/api/Brand", correlationId: corrList, bearer: plain.AccessToken);
            var create = await SendAsync(HttpMethod.Post, "/api/Brand", new { title = "Denied", code = "DN" }, corrCreate, plain.AccessToken);

            Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corrList), e => CapturingSink.SourceContext(e) == "EMS.Security"
                                                               && CapturingSink.PropertyText(e, "RequiredPermission") == "Brands.View"
                                                               && CapturingSink.PropertyText(e, "UserId") == plain.UserId.ToString());
            Assert.Contains(Sink.ForCorrelation(corrCreate), e => CapturingSink.SourceContext(e) == "EMS.Security"
                                                                 && CapturingSink.PropertyText(e, "RequiredPermission") == "Brands.Create"
                                                                 && CapturingSink.PropertyText(e, "Method") == "POST");

            // Nothing was created by the denied request.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                Assert.False(await db.Brands.AnyAsync(b => b.Title == "Denied"));
            }

            // The administrator holds every permission (granted by the seeder) and is not blocked.
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/Brand", bearer: admin.AccessToken)).StatusCode);
            var created = await SendAsync(HttpMethod.Post, "/api/Brand", new { title = "Allowed", code = "AL" }, bearer: admin.AccessToken);
            Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_role_with_only_the_View_permission_can_list_but_creating_is_denied_and_logged()
        {
            var admin = await LoginAsync(AdminUserName, AdminPassword);
            var userId = await CreateUserAsync(admin.AccessToken, "viewer1", "Passw0rd!x");

            var roleResponse = await SendAsync(HttpMethod.Post, "/api/Role", new { name = "BrandViewers" }, bearer: admin.AccessToken);
            Assert.True(roleResponse.IsSuccessStatusCode, await roleResponse.Content.ReadAsStringAsync());
            var roleJson = await ReadJsonAsync(roleResponse);
            var roleId = roleJson.TryGetProperty("roleId", out var rid) ? rid.GetInt64() : roleJson.GetProperty("id").GetInt64();

            var assign = await SendAsync(HttpMethod.Post, "/api/Role/AssignPermissions",
                new { roleName = "BrandViewers", permissions = new[] { "Brands.View" } }, bearer: admin.AccessToken);
            Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

            var setRoles = await SendAsync(HttpMethod.Put, $"/api/Users/{userId}/roles", new { roleIds = new[] { roleId } }, bearer: admin.AccessToken);
            Assert.Equal(HttpStatusCode.NoContent, setRoles.StatusCode);

            // A fresh login picks up the new role's permissions.
            var viewer = await LoginAsync("viewer1", "Passw0rd!x");
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/Brand", bearer: viewer.AccessToken)).StatusCode);

            var corr = NewCorrelationId("it-viewer-create");
            var denied = await SendAsync(HttpMethod.Post, "/api/Brand", new { title = "Nope", code = "NO" }, corr, viewer.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Contains(Sink.ForCorrelation(corr), e => CapturingSink.PropertyText(e, "RequiredPermission") == "Brands.Create");

            // The permission change itself was audited with exactly what was granted.
            Assert.Contains(Sink.Snapshot(), e => CapturingSink.SourceContext(e) == "EMS.Audit"
                                                  && CapturingSink.PropertyText(e, "Operation") == "PermissionsChanged"
                                                  && (CapturingSink.PropertyText(e, "Detail") ?? "").Contains("granted=[Brands.View]"));
        }

        [Fact]
        public async Task Anonymous_requests_get_401_without_being_reported_as_permission_denials()
        {
            var corr = NewCorrelationId("it-anon");
            var response = await SendAsync(HttpMethod.Get, "/api/Users", correlationId: corr);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var events = Sink.ForCorrelation(corr);
            Assert.DoesNotContain(events, e => Rendered(e).Contains("Access denied"));
            Assert.Contains(events, e => CapturingSink.PropertyText(e, "StatusCode") == "401" && e.Level == LogEventLevel.Warning);
        }

        [Fact]
        public async Task A_rejected_bearer_token_is_logged_without_the_token()
        {
            var corr = NewCorrelationId("it-badjwt");
            const string badToken = "eyJhbGciOiJIUzI1NiJ9.e30.bm90LWEtcmVhbC1zaWduYXR1cmU";
            var response = await SendAsync(HttpMethod.Get, "/api/Users", correlationId: corr, bearer: badToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var events = Sink.ForCorrelation(corr);
            Assert.Contains(events, e => e.Level == LogEventLevel.Warning
                                         && CapturingSink.SourceContext(e) == "EMS.Security"
                                         && Rendered(e).Contains("Authentication failed"));
            Assert.DoesNotContain(events, e => Rendered(e).Contains(badToken));
        }

        [Fact]
        public async Task Correlation_id_is_echoed_when_valid_replaced_when_not_and_present_on_log_events()
        {
            var valid = NewCorrelationId("it-corr");
            var echoed = await SendAsync(HttpMethod.Get, "/api/Users", correlationId: valid);
            Assert.Equal(valid, echoed.Headers.GetValues("X-Correlation-ID").Single());
            Assert.NotEmpty(Sink.ForCorrelation(valid));

            var invalid = await SendAsync(HttpMethod.Get, "/health/live", correlationId: "bad id with spaces!");
            var replacement = invalid.Headers.GetValues("X-Correlation-ID").Single();
            Assert.NotEqual("bad id with spaces!", replacement);
            Assert.Equal(32, replacement.Length);

            var generated = await SendAsync(HttpMethod.Get, "/health/live");
            Assert.True(generated.Headers.Contains("X-Correlation-ID"));
        }

        [Fact]
        public async Task Administrative_changes_are_audited_in_the_log_and_in_the_database_with_masked_secrets()
        {
            var admin = await LoginAsync(AdminUserName, AdminPassword);
            var corr = NewCorrelationId("it-audit");
            var response = await SendAsync(HttpMethod.Post, "/api/Users", new
            {
                userName = "audited1",
                password = "Passw0rd!x",
                firstName = "Audit",
                lastName = "Me",
                personnelCode = "PC-audited1"
            }, corr, admin.AccessToken);
            Assert.True(response.IsSuccessStatusCode);

            // 1. structured audit event naming the actor
            Assert.Contains(Sink.ForCorrelation(corr), e => CapturingSink.SourceContext(e) == "EMS.Audit"
                                                            && Rendered(e).Contains("User")
                                                            && Rendered(e).Contains("created by user " + admin.UserId));

            // 2. data-level audit trail row, written in the same request
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var rows = await db.AuditLogs.Where(a => a.CorrelationId == corr && a.Action == "Insert").ToListAsync();
            var userRow = Assert.Single(rows, r => r.NewValues!.Contains("audited1"));
            Assert.Equal(admin.UserId, userRow.UserId);
            Assert.Contains("\"PasswordHash\":\"***\"", userRow.NewValues);
            Assert.DoesNotContain("Passw0rd!x", userRow.NewValues);
        }

        [Fact]
        public async Task Health_endpoints_work_and_do_not_flood_the_request_log()
        {
            var live = await SendAsync(HttpMethod.Get, "/health/live");
            var ready = await SendAsync(HttpMethod.Get, "/health/ready");

            Assert.Equal(HttpStatusCode.OK, live.StatusCode);
            Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
            Assert.DoesNotContain(Sink.Snapshot(), e => (CapturingSink.PropertyText(e, "RequestPath") ?? "").StartsWith("/health")
                                                       && e.Level >= LogEventLevel.Information);
        }

        [Fact]
        public async Task Log_files_are_written_as_structured_json_and_security_events_have_their_own_file()
        {
            await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "file-check-user", password = "Wrong1pass" }, NewCorrelationId("it-file"));

            var mainFile = Directory.GetFiles(_factory.LogDirectory, "ems-2*.log").Single();
            var securityFile = Directory.GetFiles(_factory.LogDirectory, "ems-security-*.log").Single();

            var main = ReadLines(mainFile);
            var security = ReadLines(securityFile);

            // Compact JSON: one parseable object per line, with template and level.
            Assert.All(main.Concat(security), line => Assert.True(JsonDocument.Parse(line).RootElement.TryGetProperty("@t", out _)));

            Assert.Contains(security, l => l.Contains("Login failed"));
            Assert.DoesNotContain(security, l => l.Contains("responded"));      // request logs stay out of the security file
            Assert.Contains(main, l => l.Contains("responded"));                // ...but are in the main file
            Assert.Contains(main, l => l.Contains("Login failed"));             // main file has everything
        }

        private static List<string> ReadLines(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }
            return lines;
        }
    }
}
