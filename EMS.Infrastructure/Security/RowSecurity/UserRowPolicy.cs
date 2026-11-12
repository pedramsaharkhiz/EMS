using EMS.Application.Common.DataAccess;
using EMS.Infrastructure.Persistence.Identity;

namespace EMS.Infrastructure.Security.RowSecurity
{
    /// <summary>Users (list, Excel export). Open for now, like the policies of the other protected types (see <c>OpenRowPolicies</c>); the user detail and edits must follow the same rule later.</summary>
    public sealed class UserRowPolicy : OpenRowPolicy<ApplicationUser>
    {
        public override string Name => "User";
    }
}
