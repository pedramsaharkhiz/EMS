using EMS.Application.Dtos.SupportingDepartment;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public sealed class SupportingDepartmentOrganizationsDtoValidator : AbstractValidator<SupportingDepartmentOrganizationsDto>
    {
        public const int MaxOrganizations = 1000;
        public const int MaxGroups = 200;

        public SupportingDepartmentOrganizationsDtoValidator()
        {
            RuleFor(x => x).Must(x => Has(x.OrganizationIds) || Has(x.FieldIds) || Has(x.OrganizationManagementIds) || Has(x.ZoneIds))
                .WithName("organizationIds")
                .WithMessage("حداقل یکی از فهرست‌های شعبه، ناحیه (زمینه‌ی کاری)، مدیریت شعب یا حوزه باید مقدار داشته باشد.");

            Ids(x => x.OrganizationIds, "شعبه", MaxOrganizations);
            Ids(x => x.FieldIds, "ناحیه", MaxGroups);
            Ids(x => x.OrganizationManagementIds, "مدیریت شعب", MaxGroups);
            Ids(x => x.ZoneIds, "حوزه", MaxGroups);
        }

        private static bool Has(List<long>? ids) => ids is { Count: > 0 };

        private void Ids(System.Linq.Expressions.Expression<Func<SupportingDepartmentOrganizationsDto, List<long>?>> ids, string name, int max)
        {
            RuleFor(ids).Must(v => v is null || v.Count <= max).WithMessage($"حداکثر {max} مورد برای {name} مجاز است.");
            RuleFor(ids).Must(v => v is null || v.All(id => id > 0)).WithMessage($"شناسه {name} باید بزرگتر از صفر باشد.");
        }
    }
}
