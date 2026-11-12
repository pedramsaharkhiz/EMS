using EMS.Application.Common.DataAccess;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Security.RowSecurity;
using System.Linq.Expressions;

namespace EMS.Tests.Support
{
    /// <summary>The switch of the test scope: null = like production (everybody unrestricted); a department id = every user is restricted to that department.</summary>
    public static class RowSecurityTestState
    {
        private static long _department = -1;   // -1 = off (long? cannot be used with Volatile)
        public static long? RestrictedToDepartment
        {
            get { var value = Interlocked.Read(ref _department); return value < 0 ? null : value; }
            set => Interlocked.Exchange(ref _department, value ?? -1);
        }
    }

    public sealed class SwitchableDataScopeResolver : IDataScopeResolver
    {
        private readonly OpenDataScopeResolver _open = new();

        public DataScope Resolve(DataScopeInput input) =>
            RowSecurityTestState.RestrictedToDepartment is { } department
                ? DataScope.Restricted(input.UserId, department, $"آزمون: فقط داده‌های دپارتمان {department}")
                : _open.Resolve(input);
    }

    /// <summary>A stand-in for the real rule: a failure report is visible when the supporting department of its branch is the scope's department.</summary>
    public sealed class TestFailureReportDepartmentPolicy : RowPolicy<FailureReportRow>
    {
        public override string Name => "TestFailureReportDepartment";
        public override string Describe(DataScope scope) => $"گزارش‌هایی که دپارتمان شعبه‌شان {scope.SupportingDepartmentId} است";
        public override Expression<Func<FailureReportRow, bool>>? Filter(DataScope scope)
        {
            var department = scope.SupportingDepartmentId;
            return r => r.SupportingDepartmentId == department;
        }
    }

    /// <summary>Equipment is visible when its branch's department or the department that holds it is the scope's department.</summary>
    public sealed class TestEquipmentDepartmentPolicy : RowPolicy<EquipmentRow>
    {
        public override string Name => "TestEquipmentDepartment";
        public override string Describe(DataScope scope) => $"تجهیزهایی که دپارتمان شعبه یا دپارتمان نگه‌دارنده‌شان {scope.SupportingDepartmentId} است";
        public override Expression<Func<EquipmentRow, bool>>? Filter(DataScope scope)
        {
            var department = scope.SupportingDepartmentId;
            return r => r.OrganizationSupportingDepartmentId == department || r.SupportingDepartmentId == department;
        }
    }
}
