using System.Linq.Expressions;
using System.Reflection;

namespace EMS.Application.Common.Export
{
    /// <summary>How a column's values are written to a cell. <see cref="Auto"/> picks from the CLR type (see <see cref="ExcelCellFormatter"/>).</summary>
    public enum ExcelCellKind
    {
        /// <summary>Text, numbers, bool (بله/خیر), dates (Jalali date and time, Tehran time), enums by name.</summary>
        Auto,

        /// <summary>Always text, even for numbers: codes and serials whose leading zeros must survive.</summary>
        Text,

        /// <summary>A date column written as a Jalali date without the time.</summary>
        Date,
    }

    /// <summary>One column of an export: its Persian title, how to read the value from a row, and how to write it.</summary>
    public sealed class ExcelColumn<TRow>
    {
        public ExcelColumn(string title, string propertyName, Func<TRow, object?> value, ExcelCellKind kind)
        {
            Title = title;
            PropertyName = propertyName;
            Value = value;
            Kind = kind;
        }

        public string Title { get; }
        public string PropertyName { get; }
        public Func<TRow, object?> Value { get; }
        public ExcelCellKind Kind { get; }
    }

    /// <summary>
    /// Everything that is specific to one export: the sheet, the file name label and the columns. Written once per
    /// entity (see <c>EquipmentExport</c>) and handed to <see cref="IExcelExporter"/>, which does the rest the same way for
    /// every export in the application. Build it with <see cref="ExcelExportDefinition.For{TRow}"/>.
    /// </summary>
    public sealed class ExcelExportDefinition<TRow>
    {
        internal ExcelExportDefinition(string name, string sheetTitle, string fileLabel, IReadOnlyList<ExcelColumn<TRow>> columns)
        {
            Name = name;
            SheetTitle = sheetTitle;
            FileLabel = fileLabel;
            Columns = columns;
        }

        /// <summary>Stable English name of what is exported (used in the audit log), e.g. "Equipment".</summary>
        public string Name { get; }

        /// <summary>Persian sheet name.</summary>
        public string SheetTitle { get; }

        /// <summary>Persian label at the start of the file name: <c>تجهیزات</c> gives <c>تجهیزات-1405-08-12.xlsx</c>.</summary>
        public string FileLabel { get; }

        public IReadOnlyList<ExcelColumn<TRow>> Columns { get; }
    }

    public static class ExcelExportDefinition
    {
        /// <param name="name">Stable English name, for the audit log.</param>
        /// <param name="sheetTitle">Persian sheet name.</param>
        /// <param name="fileLabel">Persian file name label.</param>
        public static ExcelExportDefinitionBuilder<TRow> For<TRow>(string name, string sheetTitle, string fileLabel) =>
            new(name, sheetTitle, fileLabel);
    }

    public sealed class ExcelExportDefinitionBuilder<TRow>
    {
        private readonly string _name;
        private readonly string _sheetTitle;
        private readonly string _fileLabel;
        private readonly List<ExcelColumn<TRow>> _columns = new();

        internal ExcelExportDefinitionBuilder(string name, string sheetTitle, string fileLabel)
        {
            _name = name;
            _sheetTitle = sheetTitle;
            _fileLabel = fileLabel;
        }

        /// <summary>
        /// Adds a column. Without a <paramref name="title"/> the Persian title of the property name is taken from
        /// <see cref="IColumnTitleProvider"/> (the shared dictionary: Id, Code, IsActive, CreatedDateTime, ...).
        /// </summary>
        public ExcelExportDefinitionBuilder<TRow> Column<TValue>(
            Expression<Func<TRow, TValue>> value, string? title = null, ExcelCellKind kind = ExcelCellKind.Auto)
        {
            var member = (value.Body as MemberExpression)?.Member
                         ?? (value.Body is UnaryExpression { Operand: MemberExpression inner } ? inner.Member : null);
            var propertyName = member?.Name ?? throw new ArgumentException("The column must read a property of the row, e.g. x => x.YaasCode.", nameof(value));

            var getter = value.Compile();
            _columns.Add(new ExcelColumn<TRow>(title ?? string.Empty, propertyName, row => getter(row), kind));
            return this;
        }

        /// <summary>
        /// Validates and creates the definition: every column has a Persian title (own or from the shared dictionary) and no
        /// two columns of the sheet share one. A mistake fails at startup/first use, never silently in a file.
        /// </summary>
        public ExcelExportDefinition<TRow> Build(IColumnTitleProvider? titles = null)
        {
            titles ??= CommonColumnTitles.Instance;
            if (_columns.Count == 0)
                throw new InvalidOperationException($"Export '{_name}' has no columns.");

            var resolved = new List<ExcelColumn<TRow>>();
            foreach (var column in _columns)
            {
                var title = string.IsNullOrWhiteSpace(column.Title) ? titles.TitleOf(column.PropertyName) : column.Title;
                if (string.IsNullOrWhiteSpace(title))
                    throw new InvalidOperationException(
                        $"Export '{_name}': column '{column.PropertyName}' has no Persian title. Pass one to Column(...) or add it to CommonColumnTitles.");

                resolved.Add(new ExcelColumn<TRow>(title.Trim(), column.PropertyName, column.Value, column.Kind));
            }

            var duplicate = resolved.GroupBy(c => c.Title).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException($"Export '{_name}': the title '{duplicate.Key}' is used by more than one column.");

            return new ExcelExportDefinition<TRow>(_name, _sheetTitle, _fileLabel, resolved);
        }
    }
}
