namespace EMS.Domain
{
    /// <summary>Column lengths of <c>Equipment</c>, shared by the EF configuration and the validators.</summary>
    public static class EquipmentConstraints
    {
        public const string OwnerCheckName = "CK_Equipments_Owner";

        public const int YaasCodeMaxLength = 50;
        public const int SerialNumberMaxLength = 50;
        public const int AssetNumberMaxLength = 50;
        public const int DescriptionMaxLength = 500;

        /// <summary>The most equipment one bulk-create request may carry.</summary>
        public const int BulkCreateMaxItems = 200;
    }
}
