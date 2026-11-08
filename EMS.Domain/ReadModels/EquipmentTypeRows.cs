namespace EMS.Domain.ReadModels
{
    /// <summary>An equipment type with the title of its category (what the equipment form shows next to the type).</summary>
    public sealed class EquipmentTypeWithCategoryRow
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public long EquipmentCategoryId { get; set; }
        public string? EquipmentCategoryTitle { get; set; }
        /// <summary>The type has at least one active model.</summary>
        public bool HasModels { get; set; }
    }
}
