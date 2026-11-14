using EMS.Application.Common.Exceptions;
using EMS.Application.Services.Equipment;
using EMS.Domain;

namespace EMS.Infrastructure.Persistence
{
    /// <summary>
    /// The message and error code of every unique index that has a meaning for the user, by index name. When the database refuses an insert or an
    /// update because of one of them (two requests raced past the service's check), the unit of work answers with this instead of the generic
    /// "unique value exists". A test requires every unique index of the equipment table to be listed here.
    /// </summary>
    public static class UniqueConstraintMessages
    {
        public static readonly IReadOnlyDictionary<string, (string Message, string Code)> ByIndex = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [EmployeePhoneRules.UniqueIndex] = (EmployeePhoneRules.TakenMessage, EmployeePhoneRules.TakenCode),
            [UserIdentityRules.PersonnelIndex] = (UserIdentityRules.PersonnelTaken, ErrorCodes.PersonnelCodeTaken),
            [UserIdentityRules.NationalIndex] = (UserIdentityRules.NationalTaken, ErrorCodes.NationalCodeTaken),
            [EquipmentConstraints.SerialNumberIndexName] = (EquipmentMessages.SerialTaken, ErrorCodes.DuplicateSerialNumber),
            [EquipmentConstraints.AssetNumberIndexName] = (EquipmentMessages.AssetTaken, ErrorCodes.DuplicateAssetNumber),
        };

        /// <summary>The entry whose index name appears in the SQL Server error text, if any.</summary>
        public static (string Message, string Code)? Find(string? sqlErrorMessage)
        {
            if (string.IsNullOrEmpty(sqlErrorMessage)) return null;
            foreach (var (index, entry) in ByIndex)
                if (sqlErrorMessage.Contains(index, StringComparison.OrdinalIgnoreCase)) return entry;
            return null;
        }
    }
}
