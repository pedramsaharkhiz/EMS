using EMS.Application.Dtos.OrganizationHierarchy;
using FluentValidation;

namespace EMS.Application.Validators.OrganizationHierarchy;

public sealed class OrganizationHierarchyRequestDtoValidator
    : AbstractValidator<OrganizationHierarchyRequestDto>
{
    public OrganizationHierarchyRequestDtoValidator()
    {
        RuleFor(x => x.SearchLevel)
            .Must(x => OrganizationHierarchyRules.Rank(x) >= 0)
            .WithMessage(
                "سطح جستجو باید zone، organizationManagement، field یا organization باشد.");

        RuleFor(x => x.ParentLevel)
            .Must(x =>
                string.IsNullOrWhiteSpace(x) ||
                OrganizationHierarchyRules.Rank(x) >= 0)
            .WithMessage("سطح والد معتبر نیست.");

        RuleFor(x => x.ParentLevel)
            .NotEmpty()
            .When(x => x.ParentId.HasValue)
            .WithMessage(
                "در صورت ارسال شناسه والد، سطح والد نیز الزامی است.");

        RuleFor(x => x.ParentId)
            .GreaterThan(0)
            .When(x => x.ParentId.HasValue)
            .WithMessage("شناسه والد باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.ParentLevel)
            .Must((request, parentLevel) =>
                OrganizationHierarchyRules.IsValidParent(
                    request.SearchLevel,
                    parentLevel))
            .WithMessage(
                "سطح والد باید بالاتر از سطح جستجو باشد.");

        RuleFor(x => x.GroupBy)
            .Must((request, groupBy) =>
                OrganizationHierarchyRules.IsValidGroup(
                    request.SearchLevel,
                    groupBy))
            .WithMessage(
                "گروه‌بندی انتخاب‌شده برای این سطح جستجو معتبر نیست.");

        RuleFor(x => x.SortBy)
            .Must((request, sortBy) =>
                OrganizationHierarchyRules.IsValidSort(
                    request.GroupBy,
                    sortBy))
            .WithMessage(
                "ستون مرتب‌سازی برای نوع خروجی انتخاب‌شده معتبر نیست.");

        RuleFor(x => x.SortDirection)
            .Must(x =>
                OrganizationHierarchyRules.Normalize(x)
                    is "asc" or "desc")
            .WithMessage("جهت مرتب‌سازی باید asc یا desc باشد.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("اندازه صفحه باید بین ۱ تا ۱۰۰ باشد.");

        RuleFor(x => x)
            .Must(x =>
                x.PageNumber < 1 ||
                x.PageSize < 1 ||
                ((long)x.PageNumber - 1) * x.PageSize <= int.MaxValue)
            .WithMessage("شماره صفحه خارج از محدوده مجاز است.");

        RuleFor(x => x.Search)
            .MaximumLength(500)
            .WithMessage(
                "عبارت جستجو نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.Title)
            .MaximumLength(500)
            .WithMessage(
                "عنوان نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.Code)
            .MaximumLength(100)
            .WithMessage(
                "کد نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.");
    }
}
