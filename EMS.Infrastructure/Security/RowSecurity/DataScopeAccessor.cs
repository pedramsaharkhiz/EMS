using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Interfaces;
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
    /// Until the business decides who sees what, everybody is unrestricted: the whole row-security machinery is in place and costs nothing, and behaviour is unchanged.
    /// The real rule replaces this class (or the resolver registration); policies and services do not change.
    /// </summary>
    public sealed class OpenDataScopeResolver : IDataScopeResolver
    {
        public DataScope Resolve(DataScopeInput input) =>
            DataScope.Unrestricted(input.UserId, input.SupportingDepartmentId, "قاعده‌ی دسترسی سطح ردیف هنوز تعریف نشده است؛ همه چیز دیده می‌شود");
    }
}
