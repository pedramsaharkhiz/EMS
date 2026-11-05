namespace EMS.Domain
{
    /// <summary>Column lengths of <c>Equipment</c>, shared by the EF configuration and the validators.</summary>
    public static class EquipmentConstraints
    {
        public const string OwnerCheckName = "CK_Equipments_Owner";

        // Serial number and asset number are unique among ALL equipment (active or not, ignoring case); an empty value is not "taken".
        public const string SerialNumberIndexName = "UX_Equipments_SerialNumber";
        public const string AssetNumberIndexName = "UX_Equipments_AssetNumber";

        public const int YaasCodeMaxLength = 50;
        public const int SerialNumberMaxLength = 50;
        public const int AssetNumberMaxLength = 50;
        public const int DescriptionMaxLength = 500;

        /// <summary>The most equipment one bulk-create request may carry.</summary>
        public const int BulkCreateMaxItems = 200;
    }
}
