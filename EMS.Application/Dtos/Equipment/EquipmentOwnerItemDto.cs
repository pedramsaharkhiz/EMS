namespace EMS.Application.Dtos.Equipment
{
    /// <summary>One owner in the form's dropdown, in the shape of every other lookup item (the owners are fixed, so they are always active).</summary>
    public sealed class EquipmentOwnerItemDto
    {
        public int Id { get; init; }

        public string Title { get; init; } = string.Empty;

        /// <summary>The stable name of the owner (the name of <c>EquipmentOwner</c>, e.g. "Company").</summary>
        public string Code { get; init; } = string.Empty;

        public bool IsActive { get; init; } = true;
    }
}
