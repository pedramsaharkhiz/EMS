using EMS.Application.Common.Security;
using System.Text.RegularExpressions;

namespace EMS.Tests.Unit
{
    /// <summary>The "role access" screen is drawn from this catalog: a permission without a Persian name must never exist.</summary>
    public class PermissionCatalogTitlesTests
    {
        private static readonly Regex Latin = new("[A-Za-z]");

        [Fact]
        public void Every_permission_is_in_the_catalog_exactly_once_under_its_own_group()
        {
            var cataloged = PermissionCatalog.Groups.SelectMany(g => g.Permissions).ToList();

            Assert.Equal(Permissions.All.OrderBy(p => p, StringComparer.Ordinal), cataloged.Select(p => p.Name).OrderBy(p => p, StringComparer.Ordinal));
            Assert.Equal(cataloged.Count, PermissionCatalog.ByName.Count);
            Assert.All(PermissionCatalog.Groups, g => Assert.All(g.Permissions, p =>
            {
                Assert.Equal(g.Key, p.Group);
                Assert.Equal($"{p.Group}.{p.Action}", p.Name);
            }));
            Assert.All(PermissionCatalog.Groups, g => Assert.NotEmpty(g.Permissions));
        }

        [Fact]
        public void Every_group_and_permission_has_a_persian_title_and_titles_never_repeat()
        {
            Assert.All(PermissionCatalog.Groups, g =>
            {
                Assert.False(string.IsNullOrWhiteSpace(g.Title));
                Assert.False(Latin.IsMatch(g.Title), $"group {g.Key}: '{g.Title}'");
            });

            var all = PermissionCatalog.Groups.SelectMany(g => g.Permissions).ToList();
            Assert.All(all, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Title), p.Name);
                Assert.False(Latin.IsMatch(p.Title), $"{p.Name}: '{p.Title}'");
                if (p.Description is not null)
                {
                    Assert.False(string.IsNullOrWhiteSpace(p.Description), p.Name);
                    Assert.False(Latin.IsMatch(p.Description), $"{p.Name}: '{p.Description}'");
                }
            });

            // two permissions with the same title could not be told apart on the screen
            var repeated = all.GroupBy(p => p.Title).Where(g => g.Count() > 1).Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Name))}").ToList();
            Assert.True(repeated.Count == 0, string.Join(" | ", repeated));
            Assert.Equal(PermissionCatalog.Groups.Count, PermissionCatalog.Groups.Select(g => g.Title).Distinct().Count());
        }

        [Fact]
        public void The_titles_say_what_the_permission_really_does_where_the_generic_form_would_mislead()
        {
            Assert.Equal("مشاهده تجهیزات", PermissionCatalog.ByName[Permissions.Equipments.View].Title);
            Assert.Equal("جابه‌جایی تجهیز", PermissionCatalog.ByName[Permissions.Equipments.Assign].Title);
            Assert.Equal("تعیین دسترسی‌های نقش‌ها", PermissionCatalog.ByName[Permissions.Roles.ManagePermissions].Title);
            Assert.Contains("خود کاربر", PermissionCatalog.ByName[Permissions.TechnicianAssignments.Work].Description);
            Assert.Equal("تجهیزات", PermissionCatalog.Groups[0].Title);          // declaration order: equipment first
        }

        [Theory]
        [InlineData("Admin", true)]
        [InlineData("admin", true)]
        [InlineData("Administrator", false)]
        [InlineData("Operator", false)]
        [InlineData(null, false)]
        public void Only_the_administrator_role_is_a_system_role(string? name, bool expected) =>
            Assert.Equal(expected, SystemRoles.IsSystem(name));
    }
}
