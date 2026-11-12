using EMS.Application.Dtos.TechnicianAssignment;
using EMS.Application.Validators.Catalog;
using EMS.Domain;
using EMS.Domain.Enums;
using FluentValidation;

namespace EMS.Application.Validators.TechnicianAssignment
{
    internal static class AssignmentRules
    {
        public static void TitleAndPriority<T>(AbstractValidator<T> v,
            System.Linq.Expressions.Expression<Func<T, string?>> title, System.Linq.Expressions.Expression<Func<T, int>> priority)
        {
            v.RuleFor(title).NotEmpty().WithMessage("عنوان ارجاع الزامی است.");
            v.RuleFor(title).MaximumLength(TechnicianAssignmentConstraints.TitleMaxLength)
                .WithMessage($"عنوان ارجاع نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.TitleMaxLength} کاراکتر باشد.");
            v.RuleFor(priority).InclusiveBetween(FailureReportPriorities.Min, FailureReportPriorities.Max)
                .WithMessage($"اولویت ارجاع الزامی است و باید بین {FailureReportPriorities.Min} (کم) تا {FailureReportPriorities.Max} (فوری) باشد.");
        }

        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public sealed class TechnicianAssignmentCreateDtoValidator : AbstractValidator<TechnicianAssignmentCreateDto>
    {
        public TechnicianAssignmentCreateDtoValidator()
        {
            RuleFor(x => x.TechnicianUserId).GreaterThan(0).WithMessage("شناسه تکنسین باید بزرگتر از صفر باشد.");
            AssignmentRules.TitleAndPriority(this, x => x.Title, x => x.PriorityId);
            RuleFor(x => x.DeadlineDateTime)
                .Must(d => !d.HasValue || AssignmentRules.ToUtc(d.Value) > DateTime.UtcNow)
                .WithMessage("مهلت انجام کار باید در آینده باشد.");
            RuleFor(x => x.Description).MaximumLength(TechnicianAssignmentConstraints.DescriptionMaxLength)
                .WithMessage($"توضیحات ارجاع نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.DescriptionMaxLength} کاراکتر باشد.");
        }
    }

    public sealed class TechnicianAssignmentUpdateDtoValidator : AbstractValidator<TechnicianAssignmentUpdateDto>
    {
        public TechnicianAssignmentUpdateDtoValidator()
        {
            AssignmentRules.TitleAndPriority(this, x => x.Title, x => x.PriorityId);
            // the deadline is judged in the service: only a CHANGED deadline has to be in the future
            RuleFor(x => x.Description).MaximumLength(TechnicianAssignmentConstraints.DescriptionMaxLength)
                .WithMessage($"توضیحات ارجاع نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.DescriptionMaxLength} کاراکتر باشد.");
        }
    }

    public sealed class TechnicianAssignmentBulkItemDtoValidator : AbstractValidator<TechnicianAssignmentBulkItemDto>
    {
        public TechnicianAssignmentBulkItemDtoValidator()
        {
            RuleFor(x => x.TechnicianUserId).GreaterThan(0).WithMessage("شناسه تکنسین باید بزرگتر از صفر باشد.");
            RuleFor(x => x.DeadlineDateTime)
                .Must(d => !d.HasValue || AssignmentRules.ToUtc(d.Value) > DateTime.UtcNow)
                .WithMessage("مهلت انجام کار باید در آینده باشد.");
            AssignmentRules.TitleAndPriority(this, x => x.Title, x => x.PriorityId);
            RuleFor(x => x.Description).MaximumLength(TechnicianAssignmentConstraints.DescriptionMaxLength)
                .WithMessage($"توضیحات ارجاع نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.DescriptionMaxLength} کاراکتر باشد.");
        }
    }

    public sealed class TechnicianAssignmentBulkCreateDtoValidator : AbstractValidator<TechnicianAssignmentBulkCreateDto>
    {
        public TechnicianAssignmentBulkCreateDtoValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("فهرست ارجاع‌ها الزامی است و نباید خالی باشد.")
                .Must(items => items is null || items.Count <= TechnicianAssignmentConstraints.BulkMaxItems)
                .WithMessage($"در هر درخواست حداکثر {TechnicianAssignmentConstraints.BulkMaxItems} ارجاع مجاز است.");

            RuleForEach(x => x.Items)
                .NotNull().WithMessage("ردیف ارجاع نمی‌تواند خالی باشد.")
                .SetValidator(new TechnicianAssignmentBulkItemDtoValidator()!);

            // A technician has at most one open assignment per report: the same technician twice in one request is a mistake, the later rows are refused.
            RuleFor(x => x.Items).Custom((items, context) =>
            {
                if (items is null) return;
                var seen = new HashSet<long>();
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] is { TechnicianUserId: > 0 } item && !seen.Add(item.TechnicianUserId))
                        context.AddFailure($"Items[{i}].TechnicianUserId", "این تکنسین در همین درخواست تکرار شده است.");
                }
            });
        }
    }

    public sealed class TechnicianAssignmentTransitionDtoValidator : AbstractValidator<TechnicianAssignmentTransitionDto>
    {
        public TechnicianAssignmentTransitionDtoValidator() =>
            RuleFor(x => x.Note).MaximumLength(TechnicianAssignmentConstraints.NoteMaxLength)
                .WithMessage($"یادداشت نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.NoteMaxLength} کاراکتر باشد.");
    }

    public sealed class TechnicianAssignmentReasonDtoValidator : AbstractValidator<TechnicianAssignmentReasonDto>
    {
        public TechnicianAssignmentReasonDtoValidator()
        {
            RuleFor(x => x.Note).NotEmpty().WithMessage("دلیل الزامی است.");
            RuleFor(x => x.Note).MaximumLength(TechnicianAssignmentConstraints.NoteMaxLength)
                .WithMessage($"دلیل نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.NoteMaxLength} کاراکتر باشد.");
        }
    }

    public sealed class TechnicianAssignmentActionCreateDtoValidator : AbstractValidator<TechnicianAssignmentActionCreateDto>
    {
        public TechnicianAssignmentActionCreateDtoValidator()
        {
            RuleFor(x => x.ActionId).GreaterThan(0).WithMessage("شناسه اقدام باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentLocationHistoryId).Must(id => !id.HasValue || id.Value > 0)
                .WithMessage("شناسه تاریخچه‌ی مکان تجهیز باید بزرگتر از صفر باشد.");
            RuleFor(x => x.ActionDateTime)
                .Must(d => !d.HasValue || AssignmentRules.ToUtc(d.Value) <= DateTime.UtcNow + FailureReportConstraints.FutureTolerance)
                .WithMessage("زمان اقدام نمی‌تواند در آینده باشد.");
            RuleFor(x => x.Description).MaximumLength(TechnicianAssignmentConstraints.NoteMaxLength)
                .WithMessage($"توضیحات اقدام نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.NoteMaxLength} کاراکتر باشد.");
        }
    }

    public sealed class TechnicianAssignmentListRequestDtoValidator : CatalogListRequestValidator<TechnicianAssignmentListRequestDto>
    {
        private const int MaxValues = 200;

        public TechnicianAssignmentListRequestDtoValidator()
        {
            RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            Ids(x => x.FailureReportIds, "گزارش خرابی");
            Ids(x => x.TechnicianUserIds, "تکنسین");
            Ids(x => x.AssignedByUserIds, "ارجاع‌دهنده");
            Ids(x => x.StatusIds, "وضعیت");
            RuleFor(x => x.PriorityIds)
                .Must(v => v is null || (v.Length <= MaxValues && v.All(FailureReportPriorities.IsValid)))
                .WithMessage($"اولویت باید بین {FailureReportPriorities.Min} تا {FailureReportPriorities.Max} باشد.");
            RuleFor(x => x.AssignedTo).GreaterThanOrEqualTo(x => x.AssignedFrom!.Value)
                .When(x => x.AssignedFrom.HasValue && x.AssignedTo.HasValue)
                .WithMessage("پایان بازه‌ی تاریخ ارجاع نمی‌تواند قبل از شروع آن باشد.");
        }

        private void Ids(System.Linq.Expressions.Expression<Func<TechnicianAssignmentListRequestDto, long[]?>> ids, string name) =>
            RuleFor(ids).Must(v => v is null || (v.Length <= MaxValues && v.All(id => id > 0)))
                .WithMessage($"شناسه‌های {name} باید بزرگتر از صفر باشند و حداکثر {MaxValues} مورد مجاز است.");
    }
}
