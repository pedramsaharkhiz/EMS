namespace EMS.Domain
{
    /// <summary>Column lengths of <c>Equipment</c>, shared by the EF configuration and the validators.</summary>
    public static class EquipmentConstraints
    {
        public const int YaasCodeMaxLength = 50;
        public const int SerialNumberMaxLength = 50;
        public const int AssetNumberMaxLength = 50;
        public const int DescriptionMaxLength = 500;
    }
}
