namespace EMS.Application.Common.Logging
{
    /// <summary>
    /// Makes user-supplied values safe to put in log events (prevents log forging via CR/LF
    /// and other control characters, and bounds the size of attacker-controlled values).
    /// </summary>
    public static class LogSanitizer
    {
        public const int DefaultMaxLength = 100;

        public static string? Sanitize(string? value, int maxLength = DefaultMaxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var span = value.AsSpan(0, Math.Min(value.Length, maxLength));
            var chars = new char[span.Length];
            for (var i = 0; i < span.Length; i++)
                chars[i] = char.IsControl(span[i]) ? '_' : span[i];

            var result = new string(chars);
            return value.Length > maxLength ? result + "…" : result;
        }
    }
}
