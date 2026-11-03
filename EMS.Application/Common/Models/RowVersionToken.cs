using EMS.Application.Common.Exceptions;
using System.Globalization;

namespace EMS.Application.Common.Models
{
    /// <summary>
    /// Opaque version token for optimistic concurrency, derived from the row's last-modified time
    /// (no extra database column). Detail DTOs return it; update requests may send it back. When it is sent and
    /// the row was changed by someone else in the meantime, the update is rejected with 409 instead of silently
    /// overwriting the other change. When it is not sent, behaviour is unchanged (last write wins).
    /// </summary>
    public static class RowVersionToken
    {
        /// <summary>Message of the 409 returned when a record was changed by someone else after the client read it.</summary>
        public const string ConflictMessage = "این رکورد در این فاصله توسط کاربر دیگری تغییر کرده است. اطلاعات را دوباره بارگذاری کنید و تغییرات را مجدداً اعمال کنید.";

        public static string From(DateTime? modifiedDateTime) =>
            (modifiedDateTime?.Ticks ?? 0).ToString(CultureInfo.InvariantCulture);

        /// <returns>false when no token was supplied (no concurrency check requested).</returns>
        public static bool TryParse(string? token, out DateTime? expectedModifiedDateTime)
        {
            expectedModifiedDateTime = null;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (!long.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                ticks < 0 || ticks > DateTime.MaxValue.Ticks)
            {
                throw new BadRequestException("مقدار rowVersion نامعتبر است.", ErrorCodes.InvalidRowVersion);
            }

            expectedModifiedDateTime = ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Unspecified);
            return true;
        }
    }
}
