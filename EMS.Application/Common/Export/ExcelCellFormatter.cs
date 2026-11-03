using EMS.Application.Common.Globalization;
using System.Globalization;

namespace EMS.Application.Common.Export
{
    /// <summary>
    /// The value of one cell after conversion: text, or a number. <see cref="QuotePrefix"/> marks text that starts like a formula
    /// (= + - @ tab CR): it is written as a text cell with Excel's quote prefix, so no spreadsheet program ever runs it.
    /// </summary>
    public readonly record struct ExcelCellValue(string? Text, double? Number, bool QuotePrefix = false)
    {
        public static readonly ExcelCellValue Empty = new(null, null);
        public bool IsEmpty => Text is null && Number is null;
    }

    /// <summary>
    /// The one place that decides how a value is written to an Excel cell, so every export looks the same:
    /// dates in Jalali (Tehran time), booleans as بله/خیر, numbers as numbers, everything else as text.
    /// Independent of the Excel library, so it is unit tested without a workbook.
    /// </summary>
    public static class ExcelCellFormatter
    {
        public const string True = "بله";
        public const string False = "خیر";

        public static ExcelCellValue Convert(object? value, ExcelCellKind kind = ExcelCellKind.Auto)
        {
            if (value is null)
                return ExcelCellValue.Empty;

            if (kind == ExcelCellKind.Text)
                return Text(System.Convert.ToString(value, CultureInfo.InvariantCulture));

            switch (value)
            {
                case string text: return Text(text);
                case bool flag: return Text(flag ? True : False);
                case DateTime dateTime: return Text(kind == ExcelCellKind.Date ? PersianDate.ToDate(dateTime) : PersianDate.ToDateTime(dateTime));
                case DateTimeOffset offset: return Text(kind == ExcelCellKind.Date ? PersianDate.ToDate(offset) : PersianDate.ToDateTime(offset));
                case DateOnly date: return Text(PersianDate.ToDate(date));
                case Enum enumeration: return Text(enumeration.ToString());
                case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    return new ExcelCellValue(null, System.Convert.ToDouble(value, CultureInfo.InvariantCulture));
                default: return Text(System.Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Does the text start like a formula (CSV / formula injection)? Such a cell gets the quote prefix.</summary>
        public static bool LooksLikeFormula(string text) =>
            text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r';

        private static ExcelCellValue Text(string? text) =>
            string.IsNullOrEmpty(text) ? ExcelCellValue.Empty : new ExcelCellValue(text, null, LooksLikeFormula(text));
    }
}
