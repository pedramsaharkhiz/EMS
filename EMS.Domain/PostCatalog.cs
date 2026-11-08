using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The seeded system rows of the <c>Posts</c> table and the rules that identify them.</summary>
    public static class PostCatalog
    {
        /// <summary>Initial Persian titles. Administrators may rename them later; the migration only inserts them once.</summary>
        public static readonly IReadOnlyDictionary<PostCode, string> Titles = new Dictionary<PostCode, string>
        {
            [PostCode.Line] = "صف",
            [PostCode.Staff] = "ستاد",
        };

        /// <summary>The primary key of the row that represents <paramref name="code"/> (the enum value itself).</summary>
        public static long IdOf(PostCode code) => (long)code;

        /// <summary>Does <paramref name="code"/> (any casing) name one of the system posts?</summary>
        public static bool IsSystemCode(string? code) => TryGetSystemCode(code, out _);

        public static bool TryGetSystemCode(string? code, out PostCode result)
        {
            result = PostCode.Unknown;
            return !string.IsNullOrWhiteSpace(code)
                   && Enum.TryParse(code.Trim(), ignoreCase: true, out result)
                   && Titles.ContainsKey(result);
        }

        /// <summary>Is the row with this id and code one of the seeded system rows?</summary>
        public static bool IsSystemRow(long id, string? code) =>
            TryGetSystemCode(code, out var parsed) && id == IdOf(parsed) && string.Equals(code!.Trim(), parsed.ToString(), StringComparison.Ordinal);
    }
}
