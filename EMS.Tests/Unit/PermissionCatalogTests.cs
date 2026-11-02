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
        [Fact]
        public void All_is_discovered_from_every_group_without_duplicates_and_is_sorted()
        {
            var groups = typeof(Permissions).GetNestedTypes(BindingFlags.Public);
            var constants = groups
                .SelectMany(g => g.GetFields(BindingFlags.Public | BindingFlags.Static))
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToList();

            Assert.Equal(constants.Count, Permissions.All.Count);                          // nothing missing
            Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());       // a copy-pasted value would collide here
            Assert.Equal(Permissions.All.OrderBy(p => p, StringComparer.Ordinal), Permissions.All);
            Assert.True(groups.Length >= 12);
            Assert.Contains(Permissions.Brands.View, Permissions.All);
            Assert.Contains(Permissions.Roles.ManagePermissions, Permissions.All);
        }

        [Fact]
        public void Every_permission_belongs_to_the_group_that_declares_it()
        {
            // "Brands.View" must live in class Permissions.Brands: a wrong prefix would grant the wrong policy name.
            foreach (var group in typeof(Permissions).GetNestedTypes(BindingFlags.Public))
            {
                foreach (var field in group.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral && f.FieldType == typeof(string)))
                    Assert.StartsWith(group.Name + ".", (string)field.GetRawConstantValue()!);
            }
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
