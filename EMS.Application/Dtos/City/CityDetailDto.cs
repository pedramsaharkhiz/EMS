namespace EMS.Application.Dtos.City
{
    public class CityDetailDto
    {
        public long Id { get; set; }
        public long ProvinceId { get; set; }
        public string ProvinceTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
