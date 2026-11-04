using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureReport;
using EMS.Application.Validators.Catalog;
using EMS.Domain;
using FluentValidation;

namespace EMS.Application.Validators.FailureReport
{
    internal static class FailureReportRules
    {
        public const string Label = "گزارش خرابی";

        public static void Common<T>(AbstractValidator<T> v,
            System.Linq.Expressions.Expression<Func<T, long>> equipment,
            System.Linq.Expressions.Expression<Func<T, long>> failureType,
            System.Linq.Expressions.Expression<Func<T, long>> reportType,
            System.Linq.Expressions.Expression<Func<T, long>> organization,
            System.Linq.Expressions.Expression<Func<T, long?>> employee,
            System.Linq.Expressions.Expression<Func<T, string?>> referenceNumber,
            System.Linq.Expressions.Expression<Func<T, int>> priority,
            System.Linq.Expressions.Expression<Func<T, DateTime?>> reportedDateTime,
            System.Linq.Expressions.Expression<Func<T, string?>> description)
        {
            v.RuleFor(equipment).GreaterThan(0).WithMessage("شناسه تجهیز باید بزرگتر از صفر باشد.");
            v.RuleFor(failureType).GreaterThan(0).WithMessage("شناسه نوع خرابی باید بزرگتر از صفر باشد.");
            v.RuleFor(reportType).GreaterThan(0).WithMessage("شناسه نوع گزارش باید بزرگتر از صفر باشد.");
            v.RuleFor(organization).GreaterThan(0).WithMessage("شناسه شعبه باید بزرگتر از صفر باشد.");
            v.RuleFor(employee).Must(id => !id.HasValue || id.Value > 0).WithMessage("شناسه کارمند باید بزرگتر از صفر باشد.");
            v.RuleFor(referenceNumber).MaximumLength(FailureReportConstraints.ReferenceNumberMaxLength)
                .WithMessage($"شماره ارجاع {Label} نمی‌تواند بیشتر از {FailureReportConstraints.ReferenceNumberMaxLength} کاراکتر باشد.");
            v.RuleFor(priority).InclusiveBetween(FailureReportPriorities.Min, FailureReportPriorities.Max)
                .WithMessage($"اولویت {Label} باید بین {FailureReportPriorities.Min} (کم) تا {FailureReportPriorities.Max} (فوری) باشد.");
            v.RuleFor(reportedDateTime).Must(d => !d.HasValue || ToUtc(d.Value) <= DateTime.UtcNow + FailureReportConstraints.FutureTolerance)
                .WithMessage("زمان گزارش نمی‌تواند در آینده باشد.");
            v.RuleFor(description).MaximumLength(FailureReportConstraints.DescriptionMaxLength)
                .WithMessage($"توضیحات {Label} نمی‌تواند بیشتر از {FailureReportConstraints.DescriptionMaxLength} کاراکتر باشد.");
        }

        /// <summary>A time without an offset is taken as UTC (the API speaks UTC).</summary>
        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public sealed class CreateFailureReportDtoValidator : AbstractValidator<FailureReportCreateDto>
    {
        public CreateFailureReportDtoValidator() =>
            FailureReportRules.Common(this, x => x.EquipmentId, x => x.FailureTypeId, x => x.ReportTypeId, x => x.OrganizationId,
                x => x.EmployeeId, x => x.ReferenceNumber, x => x.PriorityId, x => x.ReportedDateTime, x => x.Description);
    }

    public sealed class UpdateFailureReportDtoValidator : AbstractValidator<FailureReportUpdateDto>
    {
        public UpdateFailureReportDtoValidator()
        {
            FailureReportRules.Common(this, x => x.EquipmentId, x => x.FailureTypeId, x => x.ReportTypeId, x => x.OrganizationId,
                x => x.EmployeeId, x => x.ReferenceNumber, x => x.PriorityId, x => x.ReportedDateTime, x => x.Description);
            RuleFor(x => x.FailureReportStatusId).GreaterThan(0).WithMessage("شناسه وضعیت گزارش خرابی باید بزرگتر از صفر باشد.");
        }
    }

    public sealed class FailureReportListRequestDtoValidator : CatalogListRequestValidator<FailureReportListRequestDto>
    {
        private const int MaxValues = 200;

        public FailureReportListRequestDtoValidator()
        {
            RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.ReferenceNumber).MaximumLength(100).WithMessage("شماره ارجاع جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");

            Ids(x => x.EquipmentIds, "تجهیز");
            Ids(x => x.FailureTypeIds, "نوع خرابی");
            Ids(x => x.FailureCategoryIds, "دسته‌ی خرابی");
            Ids(x => x.ReportTypeIds, "نوع گزارش");
            Ids(x => x.StatusIds, "وضعیت");
            Ids(x => x.OrganizationIds, "شعبه");
            Ids(x => x.EmployeeIds, "کارمند");
            Ids(x => x.ReportedByUserIds, "ثبت‌کننده");
            RuleFor(x => x.PriorityIds)
                .Must(v => v is null || (v.Length <= MaxValues && v.All(FailureReportPriorities.IsValid)))
                .WithMessage($"اولویت باید بین {FailureReportPriorities.Min} تا {FailureReportPriorities.Max} باشد.");

            RuleFor(x => x.ReportedTo).GreaterThanOrEqualTo(x => x.ReportedFrom!.Value)
                .When(x => x.ReportedFrom.HasValue && x.ReportedTo.HasValue)
                .WithMessage("پایان بازه‌ی تاریخ گزارش نمی‌تواند قبل از شروع آن باشد.");
        }

        private void Ids(System.Linq.Expressions.Expression<Func<FailureReportListRequestDto, long[]?>> ids, string name) =>
            RuleFor(ids).Must(v => v is null || (v.Length <= MaxValues && v.All(id => id > 0)))
                .WithMessage($"شناسه‌های {name} باید بزرگتر از صفر باشند و حداکثر {MaxValues} مورد مجاز است.");
    }
}
