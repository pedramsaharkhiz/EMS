using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace EMS.Infrastructure.Security.RowSecurity
{
    /// <summary>Where the token validation leaves what the scope needs (so that the scope costs no extra database read).</summary>
    public static class DataScopeKeys
    {
        public const string SupportingDepartmentId = "EMS.DataScope.SupportingDepartmentId";
    }

    /// <summary>
    /// The scope of the current request. A signed-in user is judged by <see cref="IDataScopeResolver"/> from what token validation already read (his supporting
    /// department); without a request user (start-up, background jobs) the scope is the system's.
    /// </summary>
    public sealed class DataScopeAccessor : IDataScopeAccessor
    {
        private readonly IHttpContextAccessor _http;
        private readonly ICurrentUserService _currentUser;
        private readonly IDataScopeResolver _resolver;
        private DataScope? _current;

        public DataScopeAccessor(IHttpContextAccessor http, ICurrentUserService currentUser, IDataScopeResolver resolver)
        {
            _http = http;
            _currentUser = currentUser;
            _resolver = resolver;
        }

        public DataScope Current => _current ??= Build();

        private DataScope Build()
        {
            var context = _http.HttpContext;
            if (context is null || !_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
                return DataScope.System;

            long? department = context.Items.TryGetValue(DataScopeKeys.SupportingDepartmentId, out var value) && value is long id ? id : null;
            return _resolver.Resolve(new DataScopeInput(userId, department, _currentUser.Roles.ToList(), _currentUser.Permissions.ToList()));
        }
    }

    /// <summary>
    /// The rule (BUSINESS-DECISIONS 3.21): whoever holds <see cref="Permissions.DataScope.ViewAll"/> (the central department's role, and the Admin role, which holds every
    /// permission) is unrestricted; everybody else is restricted to his own supporting department. A user without a department is restricted too (the policies then
    /// show him only the work he was given).
    /// </summary>
    public sealed class PermissionDataScopeResolver : IDataScopeResolver
    {
        public DataScope Resolve(DataScopeInput input)
        {
            if (input.Permissions.Contains(Permissions.DataScope.ViewAll))
                return DataScope.Unrestricted(input.UserId, input.SupportingDepartmentId, "دارای دسترسی «مشاهده داده‌های همه‌ی ستادها»");

            return DataScope.Restricted(input.UserId, input.SupportingDepartmentId, input.SupportingDepartmentId is { } department
                ? $"فقط داده‌های ستاد شما (شناسه {department}) و کارهایی که به شما ارجاع داده شده"
                : "ستاد ندارید؛ فقط کارهایی که به شما ارجاع داده شده");
        }
    }
}
