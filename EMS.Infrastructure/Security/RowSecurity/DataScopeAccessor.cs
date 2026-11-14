using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace EMS.Infrastructure.Security.RowSecurity
{
    /// <summary>Where the token validation leaves what the scope needs (so that the scope costs no extra database read).</summary>
    public static class DataScopeKeys
    {
        public const string SupportingDepartmentIds = "EMS.DataScope.SupportingDepartmentIds";
    }

    /// <summary>
    /// The scope of the current request. A signed-in user is judged by <see cref="IDataScopeResolver"/> from what token validation already read (his supporting
    /// departments); without a request user (start-up, background jobs) the scope is the system's.
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

            IReadOnlyList<long> departments = context.Items.TryGetValue(DataScopeKeys.SupportingDepartmentIds, out var value) && value is IReadOnlyList<long> ids ? ids : Array.Empty<long>();
            return _resolver.Resolve(new DataScopeInput(userId, departments, _currentUser.Roles.ToList(), _currentUser.Permissions.ToList()));
        }
    }

    /// <summary>
    /// The rule (BUSINESS-DECISIONS 3.23): whoever holds <see cref="Permissions.DataScope.ViewAll"/> (the central department's role, and the Admin role, which holds every
    /// permission) is unrestricted; everybody else is restricted to his own supporting departments. A user without a department is restricted too (the policies then
    /// show him only the work he was given).
    /// </summary>
    public sealed class PermissionDataScopeResolver : IDataScopeResolver
    {
        public DataScope Resolve(DataScopeInput input)
        {
            if (input.Permissions.Contains(Permissions.DataScope.ViewAll))
                return DataScope.Unrestricted(input.UserId, input.SupportingDepartmentIds, "دارای دسترسی «مشاهده داده‌های همه‌ی ستادها»");

            return DataScope.Restricted(input.UserId, input.SupportingDepartmentIds, input.SupportingDepartmentIds.Count switch
            {
                0 => "ستاد ندارید؛ فقط کارهایی که به شما ارجاع داده شده",
                1 => $"فقط داده‌های ستاد شما (شناسه {input.SupportingDepartmentIds[0]}) و کارهایی که به شما ارجاع داده شده",
                _ => $"فقط داده‌های ستادهای شما (شناسه‌ها: {string.Join("، ", input.SupportingDepartmentIds)}) و کارهایی که به شما ارجاع داده شده",
            });
        }
    }
}
