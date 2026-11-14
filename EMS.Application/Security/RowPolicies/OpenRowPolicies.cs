using EMS.Application.Common.DataAccess;
using EMS.Domain.ReadModels;
using System.Linq.Expressions;

namespace EMS.Application.Security.RowPolicies
{
    // THE rules of what a user sees (BUSINESS-DECISIONS 3.23). Who is unrestricted is decided once, by IDataScopeResolver (DataScope.ViewAll or the Admin role);
    // everybody else is restricted to their supporting departments (one or several), plus the work they were given. A user without a department sees only that work.
    // Each policy is only asked for a RESTRICTED scope. Keep the departments in a local array: it becomes a SQL parameter.

    /// <summary>Equipment: held by a branch of the user's department, or by the department itself; or the equipment of a report the user was assigned.</summary>
    public sealed class EquipmentRowPolicy : RowPolicy<EquipmentRow>
    {
        public override string Name => "Equipment";

        public override string Describe(DataScope scope) => !scope.HasDepartments
            ? "فقط تجهیزِ گزارش‌هایی که به شما ارجاع داده شده است (ستاد ندارید)"
            : "تجهیزی که نزد شعبه‌های زیر پوشش ستادهای شما یا نزد خود ستادهای شماست، و تجهیزِ گزارش‌هایی که به شما ارجاع داده شده است";

        public override Expression<Func<EquipmentRow, bool>>? Filter(DataScope scope)
        {
            if (!scope.HasDepartments)
                return r => r.HasAssignmentOfCurrentUser;
            var departments = scope.SupportingDepartmentIds.ToArray();
            return r => (r.OrganizationSupportingDepartmentId != null && departments.Contains(r.OrganizationSupportingDepartmentId.Value))
                        || (r.SupportingDepartmentId != null && departments.Contains(r.SupportingDepartmentId.Value))
                        || r.HasAssignmentOfCurrentUser;
        }
    }

    /// <summary>Failure reports: of a branch under the user's department, or reports the user was assigned.</summary>
    public sealed class FailureReportRowPolicy : RowPolicy<FailureReportRow>
    {
        public override string Name => "FailureReport";

        public override string Describe(DataScope scope) => !scope.HasDepartments
            ? "فقط گزارش‌هایی که به شما ارجاع داده شده است (ستاد ندارید)"
            : "گزارش‌های شعبه‌های زیر پوشش ستادهای شما، و گزارش‌هایی که به شما ارجاع داده شده است";

        public override Expression<Func<FailureReportRow, bool>>? Filter(DataScope scope)
        {
            if (!scope.HasDepartments)
                return r => r.HasAssignmentOfCurrentUser;
            var departments = scope.SupportingDepartmentIds.ToArray();
            return r => (r.SupportingDepartmentId != null && departments.Contains(r.SupportingDepartmentId.Value)) || r.HasAssignmentOfCurrentUser;
        }
    }

    /// <summary>Assignments: those of a report under the user's department, and always the user's own (he is the technician).</summary>
    public sealed class TechnicianAssignmentRowPolicy : RowPolicy<TechnicianAssignmentRow>
    {
        public override string Name => "TechnicianAssignment";

        public override string Describe(DataScope scope) => !scope.HasDepartments
            ? "فقط ارجاع‌هایی که خودِ شما تکنسین آن‌ها هستید"
            : "ارجاع‌های گزارش‌های شعبه‌های زیر پوشش ستادهای شما، و ارجاع‌هایی که خودِ شما تکنسین آن‌ها هستید";

        public override Expression<Func<TechnicianAssignmentRow, bool>>? Filter(DataScope scope)
        {
            if (!scope.HasDepartments)
                return r => r.IsOfCurrentUser;
            var departments = scope.SupportingDepartmentIds.ToArray();
            return r => r.IsOfCurrentUser || (r.FailureReportSupportingDepartmentId != null && departments.Contains(r.FailureReportSupportingDepartmentId.Value));
        }
    }

    /// <summary>Users as a choice (technician of an equipment or an assignment, existence checks): the users of the user's department, and himself.</summary>
    public sealed class UserDirectoryRowPolicy : RowPolicy<UserDirectoryRow>
    {
        public override string Name => "UserDirectory";

        public override string Describe(DataScope scope) => !scope.HasDepartments
            ? "فقط خودتان (ستاد ندارید)"
            : "کاربرانی که دست‌کم در یکی از ستادهای شما هستند";

        public override Expression<Func<UserDirectoryRow, bool>>? Filter(DataScope scope)
        {
            var self = scope.UserId ?? 0;
            if (!scope.HasDepartments)
                return u => u.Id == self;
            var departments = scope.SupportingDepartmentIds.ToArray();
            return u => u.Id == self || u.SupportingDepartmentIds.Any(d => departments.Contains(d));
        }
    }

    /// <summary>The technician dropdown: the users of the user's department, and himself.</summary>
    public sealed class TechnicianInfoRowPolicy : RowPolicy<TechnicianInfo>
    {
        public override string Name => "TechnicianChoice";

        public override string Describe(DataScope scope) => !scope.HasDepartments
            ? "فقط خودتان (ستاد ندارید)"
            : "تکنسین‌هایی که دست‌کم در یکی از ستادهای شما هستند";

        public override Expression<Func<TechnicianInfo, bool>>? Filter(DataScope scope)
        {
            var self = scope.UserId ?? 0;
            if (!scope.HasDepartments)
                return t => t.Id == self;
            var departments = scope.SupportingDepartmentIds.ToArray();
            return t => t.Id == self || t.SupportingDepartmentIds.Any(d => departments.Contains(d));
        }
    }
}
