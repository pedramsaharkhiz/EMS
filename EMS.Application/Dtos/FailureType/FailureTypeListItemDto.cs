namespace EMS.Application.Dtos.FailureType
{
    /// <summary>One equipment type a failure type applies to (only active equipment types are listed).</summary>
    public class FailureTypeEquipmentTypeDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class FailureTypeListItemDto
    {
        public long Id { get; set; }
        public long FailureCategoryId { get; set; }
        public string? FailureCategoryTitle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        /// <summary>The equipment types it applies to; empty = it applies to every equipment type.</summary>
        public List<FailureTypeEquipmentTypeDto> EquipmentTypes { get; set; } = new();
    }
}
