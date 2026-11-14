namespace EMS.Application.Dtos.Equipment
{
    /// <summary>Minimal equipment information for an organization's equipment list.</summary>
    public sealed class OrganizationEquipmentListItemDto
    {
        /// <summary>The equipment identifier.</summary>
        public long Id { get; init; }

        /// <summary>The equipment's Yaas code.</summary>
        public string YaasCode { get; init; } = string.Empty;
        public bool HasConflict { get; init; }

        /// <summary>The device serial number, when available.</summary>
        public string? SerialNumber { get; init; }

        /// <summary>The asset number, when available.</summary>
        public string? AssetNumber { get; init; }
    }
}
