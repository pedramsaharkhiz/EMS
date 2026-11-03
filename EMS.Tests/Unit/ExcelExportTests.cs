using ClosedXML.Excel;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Logging;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Services.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EMS.Tests.Unit
{
    public class ExcelExportTests
    {
        // ------------------------------------------------------------------------ definition

        private sealed record Row(long Id, string Code, string? Name, bool IsActive, DateTime CreatedDateTime, decimal? Price, DateTime? Due);

        private static ExcelExportDefinitionBuilder<Row> Builder() => ExcelExportDefinition.For<Row>("Row", "ردیف‌ها", "ردیف");

        [Fact]
        public void A_column_without_a_title_takes_it_from_the_shared_dictionary()
        {
            var definition = Builder().Column(x => x.Id).Column(x => x.IsActive).Column(x => x.CreatedDateTime).Build();
            Assert.Equal(new[] { "شناسه", "فعال", "تاریخ ایجاد" }, definition.Columns.Select(c => c.Title).ToArray());
        }

        [Fact]
        public void An_own_title_wins_over_the_dictionary()
        {
            var definition = Builder().Column(x => x.Id, "شماره").Build();
            Assert.Equal("شماره", Assert.Single(definition.Columns).Title);
        }

        [Fact]
        public void A_column_without_any_title_fails_when_the_definition_is_built()
        {
            var error = Assert.Throws<InvalidOperationException>(() => Builder().Column(x => x.Name).Build());
            Assert.Contains("Name", error.Message);
        }

        [Fact]
        public void Duplicate_titles_and_empty_definitions_are_rejected()
        {
            Assert.Throws<InvalidOperationException>(() => Builder().Column(x => x.Id).Column(x => x.Code, "شناسه").Build());
            Assert.Throws<InvalidOperationException>(() => Builder().Build());
        }

        [Fact]
        public void A_column_must_read_a_property()
        {
            Assert.Throws<ArgumentException>(() => Builder().Column(x => x.Id + 1, "x"));
        }

        [Fact]
        public void Every_common_title_is_persian_text()
        {
            foreach (var name in new[] { "Id", "Code", "Title", "Description", "IsActive", "CreatedDateTime", "ModifiedDateTime" })
                Assert.Matches("[؀-ۿ]", CommonColumnTitles.Instance.TitleOf(name)!);
            Assert.Null(CommonColumnTitles.Instance.TitleOf("Whatever"));
        }

        // ------------------------------------------------------------------------ cell formatting

        [Fact]
        public void Values_are_converted_the_same_way_in_every_export()
        {
            Assert.True(ExcelCellFormatter.Convert(null).IsEmpty);
            Assert.True(ExcelCellFormatter.Convert("").IsEmpty);
            Assert.Equal("متن", ExcelCellFormatter.Convert("متن").Text);
            Assert.Equal(ExcelCellFormatter.True, ExcelCellFormatter.Convert(true).Text);
            Assert.Equal(ExcelCellFormatter.False, ExcelCellFormatter.Convert(false).Text);
            Assert.Equal(12.5, ExcelCellFormatter.Convert(12.5m).Number);
            Assert.Equal(7, ExcelCellFormatter.Convert(7L).Number);
            Assert.Equal("Lost", ExcelCellFormatter.Convert(EMS.Domain.Enums.EquipmentStatusCode.Lost).Text);
        }

        [Fact]
        public void Dates_become_jalali_text_in_tehran_time_and_a_date_kind_drops_the_time()
        {
            var utc = new DateTime(2025, 3, 20, 21, 0, 0, DateTimeKind.Utc);
            Assert.Equal("1404/01/01 00:30", ExcelCellFormatter.Convert(utc).Text);
            Assert.Equal("1404/01/01", ExcelCellFormatter.Convert(utc, ExcelCellKind.Date).Text);
            Assert.Equal("1404/01/01", ExcelCellFormatter.Convert(new DateOnly(2025, 3, 21)).Text);
        }

        [Fact]
        public void The_text_kind_keeps_leading_zeros_of_numbers()
        {
            Assert.Equal("000123", ExcelCellFormatter.Convert("000123", ExcelCellKind.Text).Text);
            Assert.Equal("123", ExcelCellFormatter.Convert(123L, ExcelCellKind.Text).Text);
            Assert.Null(ExcelCellFormatter.Convert(123L, ExcelCellKind.Text).Number);
        }

        [Theory]
        [InlineData("=SUM(A1:A2)", true)]
        [InlineData("+1", true)]
        [InlineData("-cmd", true)]
        [InlineData("@x", true)]
        [InlineData("\tx", true)]
        [InlineData("normal", false)]
        [InlineData("a=b", false)]
        public void Text_that_looks_like_a_formula_is_marked_for_the_quote_prefix(string text, bool prefixed)
        {
            var cell = ExcelCellFormatter.Convert(text);
            Assert.Equal(text, cell.Text);                  // the data itself is never altered
            Assert.Equal(prefixed, cell.QuotePrefix);
        }

        // ------------------------------------------------------------------------ the workbook

        private sealed class NullAudit : IAuditEventLogger
        {
            public List<(string Operation, string Entity, string? Detail)> Operations { get; } = new();
            public void Created(string entityType, object? entityId) { }
            public void Updated(string entityType, object? entityId) { }
            public void Deleted(string entityType, object? entityId) { }
            public void Operation(string operation, string entityType, object? entityId, string? detail = null) => Operations.Add((operation, entityType, detail));
        }

        // The audit interceptor is only passed to the constructor, not added to the options, so the dates set below are kept as they are.
        private static EMSDbContext NewContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options,
                new AuditSaveChangesInterceptor(new FakeCurrentUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));

        private sealed class FakeCurrentUser : EMS.Application.Common.Interfaces.ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "test";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private sealed record BrandRow(long Id, string Title, string? Code, bool IsActive, DateTime CreatedDateTime);

        private static readonly ExcelExportDefinition<BrandRow> BrandExport = ExcelExportDefinition
            .For<BrandRow>("Brand", "برندها", "برند")
            .Column(x => x.Id)
            .Column(x => x.Title, "نام برند")
            .Column(x => x.Code, kind: ExcelCellKind.Text)
            .Column(x => x.IsActive)
            .Column(x => x.CreatedDateTime)
            .Build();

        private static async Task<EMSDbContext> SeedAsync(int count)
        {
            var db = NewContext();
            for (var i = 1; i <= count; i++)
                db.Brands.Add(new EMS.Domain.Entities.Brand { Title = $"Brand {i}", Code = i % 2 == 0 ? "000" + i : null, CreatedByUserId = 1, CreatedDateTime = new DateTime(2025, 3, 20, 21, 0, 0, DateTimeKind.Utc) });
            await db.SaveChangesAsync();
            return db;
        }

        private static IQueryable<BrandRow> Rows(EMSDbContext db) =>
            db.Brands.OrderBy(x => x.BrandId).Select(x => new BrandRow(x.BrandId, x.Title, x.Code, x.IsActive, x.CreatedDateTime));

        private static ExcelExporter Exporter(int maxRows = 50_000, int maxConcurrent = 2, NullAudit? audit = null)
        {
            var options = Microsoft.Extensions.Options.Options.Create(new ExportOptions { MaxRows = maxRows, MaxConcurrent = maxConcurrent });
            return new ExcelExporter(options, new ExportGate(options), audit ?? new NullAudit());
        }

        [Fact]
        public async Task The_file_has_a_right_to_left_persian_sheet_with_titles_values_and_jalali_dates()
        {
            await using var db = await SeedAsync(4);
            var audit = new NullAudit();
            var file = await Exporter(audit: audit).ExportAsync(Rows(db), BrandExport);

            Assert.Matches(@"^برند-\d{4}-\d{2}-\d{2}\.xlsx$", file.FileName);

            using var workbook = new XLWorkbook(new MemoryStream(file.Content));
            var sheet = workbook.Worksheet(1);
            Assert.Equal("برندها", sheet.Name);
            Assert.True(sheet.RightToLeft);
            Assert.Equal(new[] { "شناسه", "نام برند", "کد", "فعال", "تاریخ ایجاد" }, Enumerable.Range(1, 5).Select(c => sheet.Cell(1, c).GetString()).ToArray());
            Assert.True(sheet.Cell(1, 1).Style.Font.Bold);

            Assert.Equal(1, sheet.Cell(2, 1).GetDouble());                       // numbers stay numbers
            Assert.Equal("Brand 1", sheet.Cell(2, 2).GetString());
            Assert.True(sheet.Cell(2, 3).IsEmpty());                              // null code: empty cell
            Assert.Equal("0002", sheet.Cell(3, 3).GetString());                  // "000" + 2: leading zeros kept
            Assert.Equal("بله", sheet.Cell(2, 4).GetString());
            Assert.Equal("1404/01/01 00:30", sheet.Cell(2, 5).GetString());       // 2025-03-20 21:00 UTC in Jalali Tehran time
            Assert.Equal(XLDataType.Text, sheet.Cell(2, 5).DataType);
            Assert.Equal(5, sheet.LastRowUsed()!.RowNumber());                    // header + 4 rows

            Assert.Equal(("Exported", "Brand", "rows=4"), audit.Operations.Single());
        }

        [Fact]
        public async Task Codes_keep_their_leading_zeros_as_text_and_formula_like_text_is_never_a_formula()
        {
            await using var db = await SeedAsync(0);
            db.Brands.Add(new EMS.Domain.Entities.Brand { Title = "=1+1", Code = "000123", CreatedByUserId = 1, CreatedDateTime = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var file = await Exporter().ExportAsync(Rows(db), BrandExport);

            using var workbook = new XLWorkbook(new MemoryStream(file.Content));
            var sheet = workbook.Worksheet(1);
            Assert.Equal("000123", sheet.Cell(2, 3).GetString());
            Assert.Equal(XLDataType.Text, sheet.Cell(2, 3).DataType);
            Assert.False(sheet.Cell(2, 2).HasFormula);
            Assert.Equal("=1+1", sheet.Cell(2, 2).GetString());                   // data unchanged
            Assert.True(sheet.Cell(2, 2).Style.IncludeQuotePrefix);
        }

        [Fact]
        public async Task An_empty_result_still_produces_a_valid_file_with_the_header()
        {
            await using var db = await SeedAsync(0);
            var file = await Exporter().ExportAsync(Rows(db), BrandExport);

            using var workbook = new XLWorkbook(new MemoryStream(file.Content));
            Assert.Equal("شناسه", workbook.Worksheet(1).Cell(1, 1).GetString());
            Assert.Equal(1, workbook.Worksheet(1).LastRowUsed()!.RowNumber());
        }

        [Fact]
        public async Task More_rows_than_the_cap_is_refused_not_truncated()
        {
            await using var db = await SeedAsync(5);

            var error = await Assert.ThrowsAsync<BadRequestException>(() => Exporter(maxRows: 4).ExportAsync(Rows(db), BrandExport));
            Assert.Equal(ErrorCodes.ExportTooLarge, error.Code);
            Assert.Contains("5", error.Message);

            // exactly the cap is fine
            Assert.NotEmpty((await Exporter(maxRows: 5).ExportAsync(Rows(db), BrandExport)).Content);
        }

        [Fact]
        public async Task Only_the_configured_number_of_exports_run_at_once_and_the_slot_is_released_afterwards()
        {
            await using var db = await SeedAsync(1);
            var options = Microsoft.Extensions.Options.Options.Create(new ExportOptions { MaxConcurrent = 1 });
            var gate = new ExportGate(options);
            var exporter = new ExcelExporter(options, gate, new NullAudit());

            var held = gate.TryEnter();
            Assert.NotNull(held);
            var busy = await Assert.ThrowsAsync<TooManyRequestsException>(() => exporter.ExportAsync(Rows(db), BrandExport));
            Assert.Equal(ErrorCodes.ExportBusy, busy.Code);

            held!.Dispose();
            Assert.NotEmpty((await exporter.ExportAsync(Rows(db), BrandExport)).Content);
            Assert.NotNull(gate.TryEnter());                                      // released again after the export
        }

        [Fact]
        public async Task A_failed_export_releases_its_slot()
        {
            await using var db = await SeedAsync(5);
            var options = Microsoft.Extensions.Options.Options.Create(new ExportOptions { MaxRows = 1, MaxConcurrent = 1 });
            var gate = new ExportGate(options);
            var exporter = new ExcelExporter(options, gate, new NullAudit());

            await Assert.ThrowsAsync<BadRequestException>(() => exporter.ExportAsync(Rows(db), BrandExport));
            Assert.NotNull(gate.TryEnter());
        }

        [Theory]
        [InlineData("برندها", "برندها")]
        [InlineData("a:b/c\\d?e*f[g]h", "abcdefgh")]
        [InlineData("   ", "Sheet1")]
        public void Sheet_names_are_cleaned(string title, string expected) =>
            Assert.Equal(expected, ExcelExporter.SafeSheetName(title));

        [Fact]
        public void A_long_sheet_name_is_cut_to_the_excel_limit()
        {
            Assert.Equal(31, ExcelExporter.SafeSheetName(new string('ا', 50)).Length);
        }
    }
}
