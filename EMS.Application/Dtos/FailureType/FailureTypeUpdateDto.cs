using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeUpdateDto : ICatalogUpdateRequest
    {
        public long? FailureCategoryId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
