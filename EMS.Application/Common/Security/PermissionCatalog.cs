using System.Reflection;

namespace EMS.Application.Common.Security
{
    /// <summary>One permission as an administrator reads it: the stable name the API checks, and its Persian title and description.</summary>
    public sealed record PermissionInfo(string Name, string Group, string Action, string Title, string? Description);

    /// <summary>The permissions of one resource (one nested class of <see cref="Permissions"/>), with the resource's Persian title.</summary>
    public sealed record PermissionGroupInfo(string Key, string Title, IReadOnlyList<PermissionInfo> Permissions);

    /// <summary>
    /// The Persian names of every permission, for the screen where an administrator decides what a role may do. The names themselves stay in
    /// <see cref="Permissions"/> (what the code checks); this adds what people read. A permission added to <see cref="Permissions"/> without a
    /// title here stops the application at start-up (and a unit test), so a permission can never exist without appearing on that screen.
    /// To add one: add the constant to <see cref="Permissions"/>, the resource to <see cref="GroupTitles"/> if it is new, and a line to
    /// <see cref="Overrides"/> when the generic "action + resource" title is not what it really does.
    /// </summary>
    public static class PermissionCatalog
    {
        /// <summary>Resource (the part before the dot) -> Persian title of the group.</summary>
        private static readonly IReadOnlyDictionary<string, string> GroupTitles = new Dictionary<string, string>
        {
            [nameof(Permissions.NoteCategories)] = "موضوعات یادداشت",
            [nameof(Permissions.AssignmentNotes)] = "یادداشت‌های ارجاع",
            [nameof(Permissions.Equipments)] = "تجهیزات",
            [nameof(Permissions.FailureReports)] = "گزارش‌های خرابی",
            [nameof(Permissions.TechnicianAssignments)] = "ارجاع به تکنسین",
            [nameof(Permissions.DataScope)] = "دامنه‌ی دید داده",
            [nameof(Permissions.Users)] = "کاربران",
            [nameof(Permissions.Roles)] = "نقش‌ها",
            [nameof(Permissions.Brands)] = "برندها",
            [nameof(Permissions.EquipmentCategories)] = "دسته‌بندی‌های تجهیز",
            [nameof(Permissions.EquipmentModels)] = "مدل‌های تجهیز",
            [nameof(Permissions.Posts)] = "سمت‌ها",
            [nameof(Permissions.EquipmentStatuses)] = "وضعیت‌های تجهیز",
            [nameof(Permissions.EquipmentTypes)] = "انواع تجهیز",
            [nameof(Permissions.FailureCategories)] = "دسته‌بندی‌های خرابی",
            [nameof(Permissions.FailureTypes)] = "انواع خرابی",
            [nameof(Permissions.ReportTypes)] = "انواع گزارش",
            [nameof(Permissions.Actions)] = "اقدام‌ها",
            [nameof(Permissions.FailureReportStatuses)] = "وضعیت‌های گزارش خرابی",
            [nameof(Permissions.SupportingDepartments)] = "دپارتمان‌های پشتیبانی",
            [nameof(Permissions.Provinces)] = "استان‌ها",
            [nameof(Permissions.Cities)] = "شهرها",
            [nameof(Permissions.Skills)] = "مهارت‌ها",
            [nameof(Permissions.OrganizationHierarchy)] = "ساختار سازمانی بانک",
        };

        /// <summary>Action (the part after the dot) -> the verb of the generic title "verb + resource".</summary>
        private static readonly IReadOnlyDictionary<string, string> ActionTitles = new Dictionary<string, string>
        {
            ["View"] = "مشاهده",
            ["Create"] = "ایجاد",
            ["Update"] = "ویرایش",
            ["Delete"] = "حذف",
        };

        /// <summary>Permissions whose generic title would not say what they do, or that deserve an explanation.</summary>
        private static readonly IReadOnlyDictionary<string, (string? Title, string? Description)> Overrides = new Dictionary<string, (string?, string?)>
        {
            [Permissions.AssignmentNotes.MarkSeen] = ("ثبت مشاهده یادداشت ارجاع", "ثبت اولین مشاهده؛ فقط توسط تکنسین همان ارجاع."),
            [Permissions.Equipments.View] = (null, "فهرست و جزئیات تجهیزات، تاریخچه‌ی مکان، پیوست‌ها، خروجی اکسل و کارت تجهیزات در داشبورد."),
            [Permissions.Equipments.Create] = (null, "ثبت تکی و گروهی تجهیز، با پیوست یا بدون آن."),
            [Permissions.Equipments.Update] = (null, "ویرایش مشخصات تجهیز، بازگردانی تجهیز غیرفعال و افزودن یا حذف پیوست."),
            [Permissions.Equipments.Delete] = (null, "غیرفعال‌سازی تجهیز (حذف منطقی)."),
            [Permissions.Equipments.Assign] = ("جابه‌جایی تجهیز", "تغییر شعبه، دپارتمان پشتیبانی یا تکنسین تجهیز، تکی و گروهی."),

            [Permissions.FailureReports.View] = (null, "فهرست و جزئیات گزارش‌های خرابی، خروجی اکسل و کارت گزارش‌ها در داشبورد."),
            [Permissions.FailureReports.Update] = (null, "ویرایش گزارش، تغییر وضعیت آن و بازگردانی گزارش غیرفعال."),
            [Permissions.FailureReports.Delete] = (null, "غیرفعال‌سازی گزارش خرابی (حذف منطقی)."),

            [Permissions.TechnicianAssignments.View] = ("مشاهده ارجاع‌ها", "فهرست و جزئیات ارجاع‌ها، اقدام‌های ثبت‌شده و کارت ارجاع‌ها در داشبورد."),
            [Permissions.TechnicianAssignments.Assign] = ("ارجاع گزارش خرابی به تکنسین", "ارجاع گزارش خرابی به یک تکنسین و لغو ارجاع باز."),
            [Permissions.TechnicianAssignments.Work] = ("انجام کار ارجاع", "پذیرش، رد، شروع و تکمیل ارجاع، ثبت اقدام و ثبت «دیدم»؛ فقط برای ارجاع‌هایی که به خود کاربر داده شده است."),

            [Permissions.DataScope.ViewAll] = ("مشاهده داده‌های همه‌ی ستادها", "دیدن داده‌های همه‌ی ستادهای پشتیبانی (تجهیز، گزارش خرابی، ارجاع، کاربران)، نه فقط ستاد خود؛ مخصوص ستاد مرکزی و مدیریت. کاربر بدون این دسترسی فقط داده‌های ستاد خودش و ارجاع‌های خودش را می‌بیند."),

            [Permissions.Users.View] = (null, "فهرست و جزئیات کاربران، نقش هر کاربر و خروجی اکسل."),
            [Permissions.Users.Update] = (null, "ویرایش مشخصات کاربر و تغییر کلمه عبور او."),
            [Permissions.Users.Delete] = (null, "غیرفعال‌سازی کاربر."),
            [Permissions.Users.ManageRoles] = ("تعیین نقش کاربران", "دادن نقش به کاربر یا گرفتن آن، از صفحه‌ی کاربر."),

            [Permissions.Roles.View] = (null, "فهرست نقش‌ها، دسترسی‌های هر نقش و کاربران آن."),
            [Permissions.Roles.Update] = (null, "تغییر نام نقش و فعال یا غیرفعال کردن آن."),
            [Permissions.Roles.ManagePermissions] = ("تعیین دسترسی‌های نقش‌ها", "تعیین این‌که هر نقش چه کارهایی را مجاز است؛ حساس‌ترین دسترسی سامانه."),
            [Permissions.Roles.ManageUsers] = ("تعیین کاربران نقش‌ها", "افزودن کاربر به نقش یا برداشتن او، از صفحه‌ی نقش."),

            [Permissions.Skills.View] = (null, "فهرست و جزئیات مهارت‌ها."),
            [Permissions.OrganizationHierarchy.View] = ("مشاهده ساختار سازمانی بانک", "ناحیه‌ها، مدیریت‌های شعب، حوزه‌ها، شعبه‌ها و کارمندان بانک."),
        };

        /// <summary>Every permission, grouped by resource, in the order they are declared in <see cref="Permissions"/>.</summary>
        public static IReadOnlyList<PermissionGroupInfo> Groups { get; } = Build();

        /// <summary>Every permission by its name.</summary>
        public static IReadOnlyDictionary<string, PermissionInfo> ByName { get; } =
            Groups.SelectMany(g => g.Permissions).ToDictionary(p => p.Name, StringComparer.Ordinal);

        private static IReadOnlyList<PermissionGroupInfo> Build()
        {
            var groups = new List<PermissionGroupInfo>();
            foreach (var type in typeof(Permissions).GetNestedTypes(BindingFlags.Public).OrderBy(t => t.MetadataToken))
            {
                if (!GroupTitles.TryGetValue(type.Name, out var groupTitle))
                    throw new InvalidOperationException($"Permission group '{type.Name}' has no Persian title in PermissionCatalog.GroupTitles.");

                var permissions = new List<PermissionInfo>();
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral && f.FieldType == typeof(string)).OrderBy(f => f.MetadataToken))
                {
                    var name = (string)field.GetRawConstantValue()!;
                    var action = name[(name.IndexOf('.') + 1)..];
                    Overrides.TryGetValue(name, out var custom);

                    var title = custom.Title;
                    if (title is null)
                    {
                        if (!ActionTitles.TryGetValue(action, out var verb))
                            throw new InvalidOperationException($"Permission '{name}' has no Persian title: add it to PermissionCatalog.Overrides.");
                        title = $"{verb} {groupTitle}";
                    }

                    permissions.Add(new PermissionInfo(name, type.Name, action, title, custom.Description));
                }

                groups.Add(new PermissionGroupInfo(type.Name, groupTitle, permissions));
            }
            return groups;
        }
    }

    /// <summary>Roles the application itself relies on.</summary>
    public static class SystemRoles
    {
        /// <summary>
        /// The administrator role: it always holds every permission (the start-up seed adds new ones to it), so its permissions, name and
        /// active state cannot be changed and it cannot be deleted, otherwise nobody might be left who can manage access.
        /// </summary>
        public const string Admin = "Admin";

        public static bool IsSystem(string? roleName) => string.Equals(roleName, Admin, StringComparison.OrdinalIgnoreCase);
    }
}
