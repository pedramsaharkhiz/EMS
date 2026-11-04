using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Security;
using EMS.Application.Services.Attachment;
using EMS.Domain;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace EMS.Tests.Unit
{
    public class AttachmentRulesTests
    {
        [Theory]
        [InlineData("report.pdf", "report.pdf")]
        [InlineData(@"C:\Users\me\Desktop\report.pdf", "report.pdf")]
        [InlineData("../../etc/passwd.txt", "passwd.txt")]
        [InlineData("نقشه تجهیز.PNG", "نقشه تجهیز.PNG")]
        [InlineData("  a<b>c|d:e*f?g\".txt ", "abcdefg.txt")]
        [InlineData("evil\u0000name.pdf", "evilname.pdf")]
        [InlineData("..", "")]
        [InlineData("", "")]
        public void File_names_are_stripped_of_paths_and_reserved_characters(string input, string expected) =>
            Assert.Equal(expected, AttachmentRules.SanitizeFileName(input));

        [Fact]
        public void A_very_long_file_name_is_shortened_but_keeps_its_extension()
        {
            var name = AttachmentRules.SanitizeFileName(new string('a', 500) + ".pdf");
            Assert.Equal(AttachmentRules.MaxFileNameLength, name.Length);
            Assert.EndsWith(".pdf", name);
        }

        [Theory]
        [InlineData("a.pdf", ".pdf")]
        [InlineData("a.PDF", ".pdf")]
        [InlineData("a.jpeg", ".jpeg")]
        [InlineData("a.xlsx", ".xlsx")]
        public void Allowed_extensions_are_recognized_ignoring_case(string name, string extension) =>
            Assert.Equal(extension, AttachmentRules.TypeOf(name)!.Extension);

        [Theory]
        [InlineData("a.exe")]
        [InlineData("a.html")]
        [InlineData("a.svg")]
        [InlineData("a.js")]
        [InlineData("a")]
        [InlineData("a.pdf.exe")]
        public void Other_extensions_are_not_allowed(string name) => Assert.Null(AttachmentRules.TypeOf(name));

        [Fact]
        public void The_first_bytes_must_match_the_extension()
        {
            var pdf = AttachmentRules.TypeOf("a.pdf")!;
            var png = AttachmentRules.TypeOf("a.png")!;
            var docx = AttachmentRules.TypeOf("a.docx")!;

            Assert.True(AttachmentRules.LooksLike(pdf, "%PDF-1.7 ..."u8));
            Assert.False(AttachmentRules.LooksLike(pdf, "MZ\u0090\u0000"u8));                       // an executable renamed to .pdf
            Assert.True(AttachmentRules.LooksLike(png, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 }));
            Assert.False(AttachmentRules.LooksLike(png, "%PDF-1.7"u8));
            Assert.True(AttachmentRules.LooksLike(docx, new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2 }));
            Assert.False(AttachmentRules.LooksLike(docx, "<html>"u8));
        }

        [Fact]
        public void A_text_file_may_not_contain_binary_bytes()
        {
            var txt = AttachmentRules.TypeOf("a.txt")!;
            Assert.True(AttachmentRules.LooksLike(txt, "hello\r\nسلام\tworld"u8));
            Assert.False(AttachmentRules.LooksLike(txt, new byte[] { 0x68, 0x00, 0x69 }));
            Assert.False(AttachmentRules.LooksLike(txt, new byte[] { 0x01, 0x02 }));
        }

        [Fact]
        public async Task The_limited_stream_refuses_a_file_over_the_limit_while_reading()
        {
            using var big = new MemoryStream(new byte[1000]);
            var limited = new LimitedStream(big, 600);
            var buffer = new byte[400];
            Assert.Equal(400, await limited.ReadAsync(buffer));
            var error = await Assert.ThrowsAsync<BadRequestException>(async () => await limited.ReadAsync(buffer));
            Assert.Equal(ErrorCodes.AttachmentTooLarge, error.Code);
        }

        [Fact]
        public async Task The_prefixed_stream_gives_back_the_inspected_bytes_first()
        {
            var rest = new MemoryStream(new byte[] { 4, 5, 6 });
            var stream = new PrefixedStream(new byte[] { 1, 2, 3 }, rest);
            using var all = new MemoryStream();
            await stream.CopyToAsync(all);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, all.ToArray());
        }

        [Fact]
        public void Every_attachable_entity_has_view_and_update_permissions_and_the_attribute_is_only_on_entities()
        {
            var attachable = typeof(EMS.Domain.Entities.BaseEntity).Assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<AttachableAttribute>() is not null).ToList();

            Assert.NotEmpty(attachable);
            foreach (var type in attachable)
            {
                var attribute = type.GetCustomAttribute<AttachableAttribute>()!;
                Assert.True(typeof(EMS.Domain.Entities.BaseEntity).IsAssignableFrom(type), $"{type.Name} is [Attachable] but not an entity");
                Assert.Contains(attribute.ViewPermission, Permissions.All);
                Assert.Contains(attribute.EditPermission, Permissions.All);
                Assert.False(string.IsNullOrWhiteSpace(attribute.DisplayName));
            }
        }

        [Fact]
        public void Every_attachable_entity_is_mapped_to_a_table_with_a_single_long_key()
        {
            using var db = TestDb.SqlServerModelOnly();
            var model = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
            var types = model.GetEntityTypes().Where(t => t.ClrType.GetCustomAttribute<AttachableAttribute>() is not null).ToList();

            Assert.NotEmpty(types);
            foreach (var type in types)
            {
                Assert.NotNull(type.GetTableName());
                var key = type.FindPrimaryKey()!.Properties.Single();
                Assert.Equal(typeof(long), key.ClrType);   // EntityAttachments.EntityId is a bigint
            }
        }
    }
}
