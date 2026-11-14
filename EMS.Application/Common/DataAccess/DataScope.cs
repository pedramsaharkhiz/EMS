namespace EMS.Application.Common.DataAccess
{
    /// <summary>What the signed-in user needs to be judged by a row policy: who he is and where he works.</summary>
    public sealed record DataScopeInput(
        long UserId,
        IReadOnlyList<long> SupportingDepartmentIds,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions);

    /// <summary>
    /// How much of the data the current caller may see (row-level security). It is decided ONCE per request by <see cref="IDataScopeResolver"/> and read by the row
    /// policies. <see cref="Reason"/> says why, in words, so an administrator (and the log) can answer "why does this user see only this?".
    /// </summary>
    public sealed record DataScope(long? UserId, bool IsUnrestricted, IReadOnlyList<long> SupportingDepartmentIds, string Reason)
    {
        /// <summary>No request user: start-up work and background jobs (they act for the system, not for a person).</summary>
        public static DataScope System { get; } = new(null, true, Array.Empty<long>(), "کار سیستمی (بدون کاربر درخواست‌کننده)");

        public static DataScope Unrestricted(long userId, IReadOnlyList<long> supportingDepartmentIds, string reason) => new(userId, true, supportingDepartmentIds, reason);

        public static DataScope Restricted(long userId, IReadOnlyList<long> supportingDepartmentIds, string reason) => new(userId, false, supportingDepartmentIds, reason);

        /// <summary>Does the user work for any department (a restricted user without one sees only the work he was given)?</summary>
        public bool HasDepartments => SupportingDepartmentIds.Count > 0;
    }

    /// <summary>
    /// THE place that decides the scope of a user (which department's data he sees, or everything). A new rule of the business
    /// ("the central department sees all", "a role with this permission sees all") is written here and nowhere else.
    /// </summary>
    public interface IDataScopeResolver
    {
        DataScope Resolve(DataScopeInput input);
    }

    /// <summary>The scope of the current request (resolved once, then cached for the request).</summary>
    public interface IDataScopeAccessor
    {
        DataScope Current { get; }
    }
}
