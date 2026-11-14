namespace EMS.Domain.ReadModels
{
    /// <summary>One user as the users list shows him, flattened for the Excel export (roles and skills as one text each).</summary>
    public sealed class UserRow
    {
        public long Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        public string? NationalCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        /// <summary>The titles of the user's departments, cities and provinces, each as one text separated by "، ".</summary>
        public string? SupportingDepartmentTitle { get; set; }
        public string? CityTitle { get; set; }
        public string? ProvinceTitle { get; set; }
        public string? PostTitle { get; set; }
        /// <summary>The names of the user's active roles, separated by "، " (alphabetical).</summary>
        public string RoleNames { get; set; } = string.Empty;
        /// <summary>The titles of the user's active skills, separated by "، " (alphabetical).</summary>
        public string SkillTitles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
