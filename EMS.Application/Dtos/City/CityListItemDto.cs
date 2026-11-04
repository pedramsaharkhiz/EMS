namespace EMS.Application.Dtos.City
{
    public class CityListItemDto
    {
        public long Id { get; set; }
        public long ProvinceId { get; set; }
        public string ProvinceTitle { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }
}
