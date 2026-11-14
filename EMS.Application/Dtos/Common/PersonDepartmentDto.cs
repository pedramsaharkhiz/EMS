namespace EMS.Application.Dtos.Common
{
    /// <summary>
    /// One supporting department a person works for, with its city and province. A person has a LIST of these (<c>{prefix}SupportingDepartments</c>,
    /// <c>supportingDepartments</c>): zero, one or several; ordered by title.
    /// </summary>
    public class PersonDepartmentDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long CityId { get; set; }
        public string CityTitle { get; set; } = string.Empty;
        public long ProvinceId { get; set; }
        public string ProvinceTitle { get; set; } = string.Empty;
    }
}
