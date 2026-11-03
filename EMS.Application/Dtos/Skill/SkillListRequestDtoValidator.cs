using EMS.Application.Dtos.Skill;
using FluentValidation;

namespace EMS.Application.Validators.Skill;

public sealed class SkillListRequestDtoValidator
    : AbstractValidator<SkillListRequestDto>
{
    private static readonly string[] ListSortFields =
    [
        "id",
        "title",
        "code",
        "description",
        "isactive",
        "techniciancount",
        "createddatetime",
        "modifieddatetime"
    ];

    private static readonly string[] GroupSortFields =
    [
        "key",
        "name",
        "skillcount"
    ];

    public SkillListRequestDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل 1 باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین 1 تا 100 باشد.");

        RuleFor(x => x.SortDirection)
            .Must(x => Normalize(x) is "asc" or "desc" or "dsc")
            .WithMessage("جهت مرتب‌سازی باید asc یا desc باشد.");

        RuleFor(x => x.GroupBy)
            .Must(x =>
                string.IsNullOrWhiteSpace(x) ||
                Normalize(x) is "isactive" or "code" or "hastechnician")
            .WithMessage(
                "گروه‌بندی باید isActive، code یا hasTechnician باشد.");

        RuleFor(x => x.SortBy)
            .Must((request, sortBy) =>
            {
                if (string.IsNullOrWhiteSpace(sortBy))
                {
                    return true;
                }

                var allowed = string.IsNullOrWhiteSpace(request.GroupBy)
                    ? ListSortFields
                    : GroupSortFields;

                return allowed.Contains(Normalize(sortBy));
            })
            .WithMessage("مرتب‌سازی بر اساس این ستون برای نوع خروجی انتخاب‌شده معتبر نیست.");

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("شناسه باید بزرگ‌تر از صفر باشد.")
            .When(x => x.Id.HasValue);

        RuleFor(x => x.TechnicianUserId)
            .GreaterThan(0).WithMessage("شناسه‌ی تکنسین باید بزرگ‌تر از صفر باشد.")
            .When(x => x.TechnicianUserId.HasValue);

        RuleFor(x => x.Search)
            .MaximumLength(500).WithMessage("عبارت جستجو نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.Title)
            .MaximumLength(100).WithMessage("عنوان نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");

        RuleFor(x => x.Code)
            .MaximumLength(20).WithMessage("کد نمی‌تواند بیشتر از ۲۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.MinTechnicianCount)
            .GreaterThanOrEqualTo(0).WithMessage("حداقل تعداد تکنسین نمی‌تواند منفی باشد.")
            .When(x => x.MinTechnicianCount.HasValue);

        RuleFor(x => x.MaxTechnicianCount)
            .GreaterThanOrEqualTo(0).WithMessage("حداکثر تعداد تکنسین نمی‌تواند منفی باشد.")
            .When(x => x.MaxTechnicianCount.HasValue);

        RuleFor(x => x)
            .Must(x =>
                !x.MinTechnicianCount.HasValue ||
                !x.MaxTechnicianCount.HasValue ||
                x.MinTechnicianCount.Value <= x.MaxTechnicianCount.Value)
            .WithMessage(
                "حداقل تعداد تکنسین نباید از حداکثر بیشتر باشد.");

        RuleFor(x => x)
            .Must(x =>
                !x.CreatedFrom.HasValue ||
                !x.CreatedTo.HasValue ||
                x.CreatedFrom.Value <= x.CreatedTo.Value)
            .WithMessage("بازه تاریخ ایجاد نامعتبر است.");

        RuleFor(x => x)
            .Must(x =>
                !x.ModifiedFrom.HasValue ||
                !x.ModifiedTo.HasValue ||
                x.ModifiedFrom.Value <= x.ModifiedTo.Value)
            .WithMessage("بازه تاریخ ویرایش نامعتبر است.");
    }

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
