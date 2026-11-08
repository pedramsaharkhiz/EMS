using EMS.Application.Dtos.TechnicianAssignment;
using EMS.Application.Validators.Catalog;
using EMS.Domain;
using EMS.Domain.Enums;
using FluentValidation;

namespace EMS.Application.Validators.TechnicianAssignment
{
    internal static class AssignmentRules
    {
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
            RuleFor(x => x.DeadlineDateTime)
                .Must(d => !d.HasValue || AssignmentRules.ToUtc(d.Value) > DateTime.UtcNow)
                .WithMessage("مهلت انجام کار باید در آینده باشد.");
            RuleFor(x => x.Description).MaximumLength(TechnicianAssignmentConstraints.DescriptionMaxLength)
                .WithMessage($"توضیحات ارجاع نمی‌تواند بیشتر از {TechnicianAssignmentConstraints.DescriptionMaxLength} کاراکتر باشد.");
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
            RuleFor(x => x.AssignedTo).GreaterThanOrEqualTo(x => x.AssignedFrom!.Value)
                .When(x => x.AssignedFrom.HasValue && x.AssignedTo.HasValue)
                .WithMessage("پایان بازه‌ی تاریخ ارجاع نمی‌تواند قبل از شروع آن باشد.");
        }

        private void Ids(System.Linq.Expressions.Expression<Func<TechnicianAssignmentListRequestDto, long[]?>> ids, string name) =>
            RuleFor(ids).Must(v => v is null || (v.Length <= MaxValues && v.All(id => id > 0)))
                .WithMessage($"شناسه‌های {name} باید بزرگتر از صفر باشند و حداکثر {MaxValues} مورد مجاز است.");
    }
}
