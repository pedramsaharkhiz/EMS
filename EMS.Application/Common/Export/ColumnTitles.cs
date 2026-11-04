namespace EMS.Application.Common.Export
{
    /// <summary>Where column titles come from. Swap the implementation (e.g. a resource file) to localize without touching any export.</summary>
    public interface IColumnTitleProvider
    {
        /// <summary>The Persian title for a property name, or null when there is none.</summary>
        string? TitleOf(string propertyName);
    }

    /// <summary>
    /// The shared dictionary of column names that mean the same in every export. An export's own title always wins; this
    /// is only the default for a column declared without one. Names that mean different things per entity
    /// (Title, Name, Description of something specific) do not belong here: give those their own title in the definition.
    /// </summary>
    public sealed class CommonColumnTitles : IColumnTitleProvider
    {
        public static readonly CommonColumnTitles Instance = new();

        private static readonly IReadOnlyDictionary<string, string> Titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = "شناسه",
            ["Code"] = "کد",
            ["Title"] = "عنوان",
            ["Description"] = "توضیحات",
            ["IsActive"] = "فعال",
            ["CreatedDateTime"] = "تاریخ ایجاد",
            ["ModifiedDateTime"] = "تاریخ آخرین ویرایش",
        };

        public string? TitleOf(string propertyName) => Titles.TryGetValue(propertyName, out var title) ? title : null;
    }
}
