using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace EMS.Infrastructure.Persistence.Identity
{
    public class ApplicationUser : IdentityUser<long>, IAuditableEntity
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PersonnelCode { get; set; } = null!;
        public string? NationalCode { get; set; }
        public long? PostId { get; set; }
        public bool IsActive { get; set; } = true;
        public long? CreatedByUserId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public long? ModifiedByUserId { get; set; }
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>The supporting departments the user works for (zero, one or several).</summary>
        public ICollection<UserSupportingDepartment> SupportingDepartments { get; set; } = new List<UserSupportingDepartment>();
        public Post? Post { get; set; }
        public ICollection<TechnicianSkill> TechnicianSkills { get; set; } = new List<TechnicianSkill>();
    }
}