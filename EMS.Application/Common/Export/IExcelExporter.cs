namespace EMS.Application.Common.Export
{
    public sealed record ExcelFile(byte[] Content, string FileName)
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    /// <summary>
    /// The single way the application writes an Excel file. A feature supplies a query and an
    /// <see cref="ExcelExportDefinition{TRow}"/>; layout, Jalali dates, Persian right-to-left sheet, row cap, concurrency
    /// limit and audit are the same for every export.
    /// </summary>
    public interface IExcelExporter
    {
        /// <summary>
        /// Counts the rows first and refuses (<c>400 export_too_large</c>) above <c>Export:MaxRows</c> instead of truncating;
        /// refuses with <c>429 export_busy</c> when <c>Export:MaxConcurrent</c> exports are already running.
        /// The query is streamed from the database, not loaded as a list.
        /// </summary>
        Task<ExcelFile> ExportAsync<TRow>(IQueryable<TRow> rows, ExcelExportDefinition<TRow> definition, CancellationToken cancellationToken = default);
    }
}
