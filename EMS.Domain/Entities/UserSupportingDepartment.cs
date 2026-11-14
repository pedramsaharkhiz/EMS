namespace EMS.Domain.Entities
{
    /// <summary>
    /// One supporting department a user works for. A user may belong to several departments (and a department has many users);
    /// the row exists exactly while the membership does (no soft delete: the history of changes is in the audit log).
    /// </summary>
    public class UserSupportingDepartment
    {
        public long UserId { get; set; }
        public long SupportingDepartmentId { get; set; }
    }
}
