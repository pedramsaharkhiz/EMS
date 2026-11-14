using EMS.Application.Common.DataAccess;
using EMS.Infrastructure.Persistence.Identity;
using System.Linq.Expressions;

namespace EMS.Infrastructure.Security.RowSecurity
{
    /// <summary>Users (list, detail, Excel, edits, role assignment): the users of the user's department, and himself. A user without a department sees only himself.</summary>
    public sealed class UserRowPolicy : RowPolicy<ApplicationUser>
    {
        public override string Name => "User";

        public override string Describe(DataScope scope) => scope.SupportingDepartmentId is null
            ? "فقط خودتان (ستاد ندارید)"
            : "کاربران ستاد شما";

        public override Expression<Func<ApplicationUser, bool>>? Filter(DataScope scope)
        {
            var self = scope.UserId ?? 0;
            if (scope.SupportingDepartmentId is not { } department)
                return u => u.Id == self;
            return u => u.SupportingDepartmentId == department || u.Id == self;
        }
    }
}
