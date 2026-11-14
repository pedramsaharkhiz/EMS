using EMS.Application.Common.DataAccess;
using EMS.Infrastructure.Persistence.Identity;
using System.Linq.Expressions;

namespace EMS.Infrastructure.Security.RowSecurity
{
    /// <summary>Users (list, detail, Excel, edits, role assignment): the users who share at least one department with the user, and himself. A user without a department sees only himself.</summary>
    public sealed class UserRowPolicy : RowPolicy<ApplicationUser>
    {
        public override string Name => "User";

        public override string Describe(DataScope scope) => !scope.HasDepartments
            ? "فقط خودتان (ستاد ندارید)"
            : "کاربرانی که دست‌کم در یکی از ستادهای شما هستند";

        public override Expression<Func<ApplicationUser, bool>>? Filter(DataScope scope)
        {
            var self = scope.UserId ?? 0;
            if (!scope.HasDepartments)
                return u => u.Id == self;
            var departments = scope.SupportingDepartmentIds.ToArray();
            return u => u.Id == self || u.SupportingDepartments.Any(d => departments.Contains(d.SupportingDepartmentId));
        }
    }
}
