using ClosedXML.Excel;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Globalization;
using EMS.Application.Common.Logging;
using EMS.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EMS.Infrastructure.Services.Export
{
    /// <summary>Limits the number of exports that run at once in the process (one instance, shared by every request).</summary>
    public sealed class ExportGate
    {
        private readonly SemaphoreSlim _slots;

        public ExportGate(IOptions<ExportOptions> options) =>
            _slots = new SemaphoreSlim(Math.Max(1, options.Value.MaxConcurrent));

        /// <summary>Takes a slot without waiting; null when every slot is taken.</summary>
        public IDisposable? TryEnter() => _slots.Wait(0) ? new Releaser(_slots) : null;

        private sealed class Releaser : IDisposable
        {
            private SemaphoreSlim? _slots;
            public Releaser(SemaphoreSlim slots) => _slots = slots;
            public void Dispose() => Interlocked.Exchange(ref _slots, null)?.Release();
        }
    }

    /// <summary>
    /// ClosedXML implementation of <see cref="IExcelExporter"/>. The sheet is right-to-left with a bold frozen header and
    /// a filter, column widths follow the content, codes are written as text, dates as Jalali text.
    /// </summary>
    public sealed class ExcelExporter : IExcelExporter
    {
        private const int MinWidth = 8;
        private const int MaxWidth = 60;

        private readonly ExportOptions _options;
        private readonly ExportGate _gate;
        private readonly IAuditEventLogger _audit;

        public ExcelExporter(IOptions<ExportOptions> options, ExportGate gate, IAuditEventLogger audit)
        {
            _options = options.Value;
            _gate = gate;
            _audit = audit;
        }

        public async Task<ExcelFile> ExportAsync<TRow>(IQueryable<TRow> rows, ExcelExportDefinition<TRow> definition, CancellationToken cancellationToken = default)
        {
            using var slot = _gate.TryEnter()
                ?? throw new TooManyRequestsException("چند خروجی Excel هم‌زمان در حال ساخت است؛ کمی بعد دوباره تلاش کنید.", ErrorCodes.ExportBusy);

            var count = await rows.CountAsync(cancellationToken);
            if (count > _options.MaxRows)
                throw new BadRequestException(
                    $"تعداد ردیف‌ها ({count:N0}) بیشتر از حداکثر مجاز خروجی ({_options.MaxRows:N0}) است؛ فیلتر را محدودتر کنید.",
                    ErrorCodes.ExportTooLarge);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SafeSheetName(definition.SheetTitle));
            sheet.RightToLeft = true;
            sheet.Style.Font.FontName = "Tahoma";
            sheet.Style.Font.FontSize = 10;

            var columns = definition.Columns;
            var widths = new int[columns.Count];

            for (var c = 0; c < columns.Count; c++)
            {
                var header = sheet.Cell(1, c + 1);
                header.Value = columns[c].Title;
                widths[c] = columns[c].Title.Length;
            }

            var headerRange = sheet.Range(1, 1, 1, columns.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7ECF3");
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var rowNumber = 1;
            await foreach (var row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                rowNumber++;
                for (var c = 0; c < columns.Count; c++)
                {
                    var cell = ExcelCellFormatter.Convert(columns[c].Value(row), columns[c].Kind);
                    if (cell.IsEmpty)
                        continue;

                    var target = sheet.Cell(rowNumber, c + 1);
                    if (cell.Number is { } number)
                    {
                        target.Value = number;
                        widths[c] = Math.Max(widths[c], number.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Length);
                    }
                    else
                    {
                        target.Value = cell.Text!;
                        if (cell.QuotePrefix)
                            target.Style.IncludeQuotePrefix = true;
                        widths[c] = Math.Max(widths[c], cell.Text!.Length);
                    }
                }
            }

            for (var c = 0; c < columns.Count; c++)
                sheet.Column(c + 1).Width = Math.Clamp(widths[c] * 1.2 + 2, MinWidth, MaxWidth);

            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, Math.Max(rowNumber, 1), columns.Count).SetAutoFilter();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            _audit.Operation("Exported", definition.Name, null, $"rows={rowNumber - 1}");
            return new ExcelFile(stream.ToArray(), FileNameOf(definition.FileLabel));
        }

        /// <summary>Excel sheet names are at most 31 characters and cannot contain : \ / ? * [ ].</summary>
        public static string SafeSheetName(string title)
        {
            var cleaned = new string(title.Where(ch => ch is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray()).Trim();
            if (cleaned.Length == 0)
                cleaned = "Sheet1";
            return cleaned.Length > 31 ? cleaned[..31] : cleaned;
        }

        /// <summary>Label plus today's Jalali date, e.g. <c>تجهیزات-1405-08-12.xlsx</c>.</summary>
        public static string FileNameOf(string label)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(label.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
            if (safe.Length == 0)
                safe = "export";
            return $"{safe}-{PersianDate.ToDate(DateTime.UtcNow).Replace('/', '-')}.xlsx";
        }
    }
}
