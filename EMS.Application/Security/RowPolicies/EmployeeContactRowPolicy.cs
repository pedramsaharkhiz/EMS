using EMS.Application.Common.DataAccess;
using EMS.Domain.ReadModels;
using System.Linq.Expressions;

namespace EMS.Application.Security.RowPolicies;

public sealed class EmployeeContactRowPolicy : RowPolicy<EmployeeContactRow>
{
    public override string Name => "EmployeeContact";
    public override string Describe(DataScope scope) => "شماره‌های پرسنل شعبه‌های زیر پوشش ستادهای شما";
    public override Expression<Func<EmployeeContactRow, bool>>? Filter(DataScope scope)
    {
        var departments = scope.SupportingDepartmentIds.ToArray();
        return r => r.SupportingDepartmentId != null && departments.Contains(r.SupportingDepartmentId.Value);
    }
}
