namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// Creates several equipment in one call. Every item is a complete <see cref="EquipmentCreateDto"/> (the same rules as a single
    /// create). All or nothing: if any item is refused, nothing is saved and the error lists every problem per item.
    /// </summary>
    public class EquipmentBulkCreateDto
    {
        public List<EquipmentCreateDto> Items { get; set; } = new();
    }
}
