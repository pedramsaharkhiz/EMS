namespace EMS.Domain.ReadModels
{
    /// <summary>
    /// A user as every screen shows a person: who he is, whether he is still active, his post and where he works (his supporting departments with their
    /// cities and provinces). One shape for the technician of an equipment, the one who reported a failure, the one who changed a location, the one who
    /// uploaded a file, ... so they never differ in depth.
    /// </summary>
    public sealed class PersonReference
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public long? PostId { get; set; }
        public string? PostTitle { get; set; }
        /// <summary>Where he works: every supporting department he belongs to, each with its city and province (empty when he has none).</summary>
        public List<PersonDepartment> SupportingDepartments { get; set; } = new();
    }

    /// <summary>One supporting department of a person with its city and province.</summary>
    public sealed class PersonDepartment
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public long CityId { get; set; }
        public string CityTitle { get; set; } = string.Empty;
        public long ProvinceId { get; set; }
        public string ProvinceTitle { get; set; } = string.Empty;
    }
}
