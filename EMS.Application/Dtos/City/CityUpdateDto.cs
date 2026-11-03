using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.City
{
    public class CityUpdateDto : ICatalogUpdateRequest
    {
        /// <summary>Omit to keep the current province.</summary>
        public long? ProvinceId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
