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
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.SortDirection)
            .Must(x => Normalize(x) is "asc" or "desc" or "dsc")
            .WithMessage("SortDirection باید asc یا desc باشد.");

        RuleFor(x => x.GroupBy)
            .Must(x =>
                string.IsNullOrWhiteSpace(x) ||
                Normalize(x) is "isactive" or "code" or "hastechnician")
            .WithMessage(
                "GroupBy باید isActive، code یا hasTechnician باشد.");

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
            .WithMessage("SortBy برای نوع خروجی انتخاب‌شده معتبر نیست.");

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .When(x => x.Id.HasValue);

        RuleFor(x => x.TechnicianUserId)
            .GreaterThan(0)
            .When(x => x.TechnicianUserId.HasValue);

        RuleFor(x => x.Search)
            .MaximumLength(500);

        RuleFor(x => x.Title)
            .MaximumLength(100);

        RuleFor(x => x.Code)
            .MaximumLength(20);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.MinTechnicianCount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinTechnicianCount.HasValue);

        RuleFor(x => x.MaxTechnicianCount)
            .GreaterThanOrEqualTo(0)
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
