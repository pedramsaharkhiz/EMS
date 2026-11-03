using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// A varchar column cannot hold Persian text (it turns into "?"). Columns that people type text into must be nvarchar;
    /// varchar is only for technical values (codes, hashes, IP addresses, e-mail, claims).
    /// </summary>
    public class UnicodeColumnsTests
    {
        private static readonly string[] TextColumns =
            { "Name", "NormalizedName", "Title", "DisplayName", "Description", "Address", "FirstName", "LastName", "UserName", "FullName" };

        [Fact]
        public void Columns_that_hold_human_text_are_unicode()
        {
            using var db = TestDb.SqlServerModelOnly();
            var model = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;

            var offenders = model.GetEntityTypes()
                .SelectMany(t => t.GetProperties().Select(p => (Entity: t.ClrType.Name, Property: p)))
                .Where(x => x.Property.ClrType == typeof(string) && TextColumns.Contains(x.Property.Name))
                .Where(x => x.Property.IsUnicode() == false)
                .Select(x => $"{x.Entity}.{x.Property.Name}")
                .ToList();

            Assert.True(offenders.Count == 0, "These columns are varchar but hold text people type (Persian would be lost): " + string.Join(", ", offenders));
        }
    }
}
