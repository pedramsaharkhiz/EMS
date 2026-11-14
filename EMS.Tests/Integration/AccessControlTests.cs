using EMS.Infrastructure.Identity.Seed;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Services.Identity;
using EMS.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog.Events;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Takes access away and proves it is really gone: both for tokens that are already in circulation
    /// (validated against the user's current state on every request) and for tokens issued at the next login.
    /// </summary>
    public class AccessControlTests : ApiTestBase
    {
        public AccessControlTests(EmsApiFactory factory) : base(factory) { }

        // these tests are about the exact permissions of a role
        protected override bool RolesSeeEveryDepartment => false;

        private const string Password = "Passw0rd!x";

        /// <summary>Creates a user holding a role with the given permissions; returns everything needed to act on it.</summary>
        private async Task<(string Admin, long UserId, long RoleId, string UserToken, string UserRefresh)> ArrangeAsync(
            string suffix, params string[] permissions)
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            var userName = "ac_" + suffix;
            var userId = await CreateUserAsync(admin, userName, Password);
            var roleId = await CreateRoleAsync(admin, "AcRole_" + suffix, permissions);
            await AssignRoleAsync(admin, userId, roleId);
            var login = await LoginAsync(userName, Password);
            return (admin, userId, roleId, login.AccessToken, login.RefreshToken);
        }

        [Fact]
        public async Task A_role_removed_from_a_user_stops_working_immediately_and_stays_gone_after_the_next_login()
        {
            var a = await ArrangeAsync("remove", "Skills.View");
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));

            await RemoveRoleAsync(a.Admin, a.UserId, a.RoleId);

            // The token that was issued before the removal is rejected on the very next request...
            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));
            Assert.Contains(Sink.Snapshot(), e => CapturingSink.SourceContext(e) == "EMS.Security"
                                                  && e.Level == LogEventLevel.Warning
                                                  && Rendered(e).Contains("access changed since the token was issued"));

            // ...and a new token no longer carries the removed role's permission (this used to still return 200).
            var fresh = await LoginAsync("ac_remove", Password);
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Skills", fresh.AccessToken));
        }

        [Fact]
        public async Task A_deactivated_role_no_longer_grants_its_permissions()
        {
            var a = await ArrangeAsync("deactrole", "Skills.View");

            var update = await SendAsync(HttpMethod.Put, $"/api/Role/{a.RoleId}", new { name = "AcRole_deactrole", isActive = false }, bearer: a.Admin);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));
            var fresh = await LoginAsync("ac_deactrole", Password);
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Skills", fresh.AccessToken));
        }

        [Fact]
        public async Task Changing_a_roles_permissions_takes_effect_for_its_users_without_waiting_for_token_expiry()
        {
            var a = await ArrangeAsync("perms", "Skills.View");
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, "/api/Brand", a.UserToken, new { title = "AcBrandX", code = "X" }));

            // grant one more permission
            var grant = await SendAsync(HttpMethod.Post, "/api/Role/AssignPermissions",
                new { roleName = "AcRole_perms", permissions = new[] { "Skills.View", "Brands.Create" } }, bearer: a.Admin);
            Assert.Equal(HttpStatusCode.OK, grant.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken)); // stale token
            var fresh = await LoginAsync("ac_perms", Password);
            Assert.Equal(HttpStatusCode.Created, await StatusOfAsync(HttpMethod.Post, "/api/Brand", fresh.AccessToken, new { title = "AcBrandX", code = "X" }));

            // revoke everything
            var revoke = await SendAsync(HttpMethod.Post, "/api/Role/AssignPermissions",
                new { roleName = "AcRole_perms", permissions = Array.Empty<string>() }, bearer: a.Admin);
            Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", fresh.AccessToken));
            var relogin = await LoginAsync("ac_perms", Password);
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Skills", relogin.AccessToken));
        }

        [Fact]
        public async Task A_client_recovers_from_a_rejected_token_by_refreshing_and_gets_the_new_access()
        {
            var a = await ArrangeAsync("refresh", "Skills.View");
            await RemoveRoleAsync(a.Admin, a.UserId, a.RoleId);
            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));

            var refreshed = await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = a.UserRefresh });
            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
            var newToken = (await ReadDataAsync(refreshed)).GetProperty("accessToken").GetString()!;

            // The new token is valid (the request is authenticated) but reflects the reduced access.
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Skills", newToken));
        }

        [Fact]
        public async Task A_deactivated_user_is_locked_out_at_once_even_with_an_unexpired_token()
        {
            var a = await ArrangeAsync("deactuser", "Skills.View");
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));

            var delete = await SendAsync(HttpMethod.Delete, $"/api/Users/{a.UserId}", bearer: a.Admin);
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));
            Assert.Contains(Sink.Snapshot(), e => Rendered(e).Contains("user is inactive or no longer exists"));
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "ac_deactuser", password = Password })).StatusCode);
        }

        [Fact]
        public async Task Resetting_a_password_invalidates_the_tokens_issued_before()
        {
            var a = await ArrangeAsync("pwd", "Skills.View");
            var reset = await SendAsync(HttpMethod.Put, $"/api/Users/{a.UserId}/password", new { newPassword = "N3wPassw0rd!y" }, bearer: a.Admin);
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", a.UserToken));
            var fresh = await LoginAsync("ac_pwd", "N3wPassw0rd!y");
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Skills", fresh.AccessToken));
        }

        [Fact]
        public async Task A_correctly_signed_token_without_a_security_stamp_is_rejected()
        {
            var jwt = _factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));
            var token = new JwtSecurityToken(
                jwt.Issuer, jwt.Audience,
                new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim("permission", "Skills.View") },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            var legacy = new JwtSecurityTokenHandler().WriteToken(token);

            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", legacy));
            Assert.Contains(Sink.Snapshot(), e => Rendered(e).Contains("token has no security stamp"));
        }

        [Fact]
        public async Task The_administrator_role_is_re_activated_at_startup_so_the_system_can_never_be_locked_out()
        {
            // Someone deactivates the Admin role in the database.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                var role = await db.Roles.SingleAsync(r => r.Name == "Admin");
                role.IsActive = false;
                await db.SaveChangesAsync();
            }

            // With the role inactive an administrator has no permissions at all...
            var locked = await LoginAdminAsync();
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Users", locked.AccessToken));

            // ...until the application starts again (the seeder runs at startup).
            using (var scope = _factory.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IIdentitySeeder>().SeedAsync();

            var healed = await LoginAdminAsync();
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Users", healed.AccessToken));
            Assert.Contains(Sink.Snapshot(), e => e.Level == LogEventLevel.Warning && Rendered(e).Contains("has been re-activated"));
        }

        [Fact]
        public async Task A_role_that_still_carries_a_retired_permission_can_be_saved_again_and_the_claim_disappears()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            await CreateRoleAsync(admin, "AcRole_retired", "Skills.View", "Brands.View");     // an older client still sends "Brands.View"

            var stored = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/Role/AcRole_retired/permissions", bearer: admin));
            Assert.Equal(new[] { "Skills.View" }, stored.EnumerateArray().Select(p => p.GetString()).ToArray());
        }

        [Fact]
        public async Task A_retired_permission_still_stored_on_a_role_is_neither_sent_to_the_client_nor_put_in_the_token()
        {
            var a = await ArrangeAsync("retiredclaim", "Skills.View");

            // an older role still carries the claim: written straight to the store, as it was before the permission was retired
            using (var scope = _factory.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<EMS.Infrastructure.Persistence.Identity.ApplicationRole>>();
                var role = (await roleManager.FindByIdAsync(a.RoleId.ToString()))!;
                await roleManager.AddClaimAsync(role, new Claim("permission", "Brands.View"));
            }

            static string[] PermissionsOf(System.Text.Json.JsonElement data) => data.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()!).ToArray();
            static string[] ClaimsOf(string token) => new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token).Claims
                .Where(c => c.Type == "permission").Select(c => c.Value).ToArray();

            var login = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "ac_retiredclaim", password = Password }));
            Assert.Equal(new[] { "Skills.View" }, PermissionsOf(login));
            Assert.Equal(new[] { "Skills.View" }, ClaimsOf(login.GetProperty("accessToken").GetString()!));

            var refreshed = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = login.GetProperty("refreshToken").GetString() }));
            Assert.Equal(new[] { "Skills.View" }, PermissionsOf(refreshed));
            Assert.Equal(new[] { "Skills.View" }, ClaimsOf(refreshed.GetProperty("accessToken").GetString()!));

            var me = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/Identity/me", bearer: refreshed.GetProperty("accessToken").GetString()));
            Assert.Equal(new[] { "Skills.View" }, PermissionsOf(me));
        }

        [Fact]
        public async Task The_seeder_removes_retired_permissions_from_the_administrator_role()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<EMS.Infrastructure.Persistence.Identity.ApplicationRole>>();
                var role = (await roleManager.FindByNameAsync("Admin"))!;
                await roleManager.AddClaimAsync(role, new Claim("permission", "Brands.View"));
            }

            using (var scope = _factory.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<IIdentitySeeder>().SeedAsync();

            using (var scope = _factory.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<EMS.Infrastructure.Persistence.Identity.ApplicationRole>>();
                var role = (await roleManager.FindByNameAsync("Admin"))!;
                var claims = (await roleManager.GetClaimsAsync(role)).Where(c => c.Type == "permission").Select(c => c.Value).ToList();
                Assert.DoesNotContain("Brands.View", claims);
                Assert.Contains("Skills.View", claims);
            }
        }
    }
}
