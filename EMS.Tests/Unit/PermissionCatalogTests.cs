using EMS.Application.Common.Security;
using EMS.Presentation.Controllers;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// Guards the authorization model so that access denials stay observable:
    /// a permission that is missing from <see cref="Permissions.All"/> would have no policy, and an action
    /// without a [Permission] could never produce an "Access denied" log entry.
    /// </summary>
    public class PermissionCatalogTests
    {
        private static IEnumerable<string> DeclaredPermissionConstants() =>
            typeof(Permissions).GetNestedTypes(BindingFlags.Public)
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!);

        [Fact]
        public void Every_declared_permission_is_registered_in_All_exactly_once()
        {
            var declared = DeclaredPermissionConstants().ToList();
            var all = Permissions.All;

            Assert.Empty(declared.Except(all));            // declared but forgotten in All => no policy => always 403
            Assert.Empty(all.Except(declared));            // listed in All but not declared
            Assert.Equal(all.Count, all.Distinct().Count());
        }

        [Fact]
        public void Permission_names_follow_the_Resource_Action_convention()
        {
            Assert.All(Permissions.All, p => Assert.Matches(@"^[A-Za-z]+\.[A-Za-z]+$", p));
        }

        [Fact]
        public void Every_action_of_every_controller_except_the_identity_endpoints_requires_a_known_permission()
        {
            var controllers = typeof(BaseApiController).Assembly.GetTypes()
                .Where(t => typeof(BaseApiController).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(IdentityController))
                .ToList();
            Assert.True(controllers.Count >= 10);

            var unprotected = new List<string>();
            foreach (var controller in controllers)
            {
                var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());

                foreach (var action in actions)
                {
                    var permission = action.GetCustomAttribute<PermissionAttribute>(inherit: true)
                                     ?? controller.GetCustomAttribute<PermissionAttribute>(inherit: true);
                    var allowsAnonymous = action.GetCustomAttribute<AllowAnonymousAttribute>() is not null;

                    var known = permission?.Policy is { } policy
                                && Permissions.All.Contains(policy.Replace("Permission:", string.Empty));
                    if (!known && !allowsAnonymous)
                        unprotected.Add($"{controller.Name}.{action.Name}");
                }
            }

            Assert.True(unprotected.Count == 0,
                "Actions without a registered [Permission]: " + string.Join(", ", unprotected));
        }
    }
}
