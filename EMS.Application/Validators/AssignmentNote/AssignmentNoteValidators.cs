using EMS.Application.Dtos.AssignmentNote;
using EMS.Application.Validators.Catalog;
using EMS.Domain;
using FluentValidation;

namespace EMS.Application.Validators.AssignmentNote;

public class AssignmentNoteCreateDtoValidator : AbstractValidator<AssignmentNoteCreateDto>
{
    public AssignmentNoteCreateDtoValidator()
    {
        RuleFor(x => x.NoteCategoryId).GreaterThan(0).WithMessage("شناسه موضوع یادداشت باید بزرگتر از صفر باشد.");
        RuleFor(x => x.Text).NotEmpty().WithMessage("متن یادداشت الزامی است.")
            .MaximumLength(AssignmentNoteConstraints.TextMaxLength).WithMessage("متن یادداشت نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد.");
    }
}

public sealed class AssignmentNoteUpdateDtoValidator : AbstractValidator<AssignmentNoteUpdateDto>
{
    public AssignmentNoteUpdateDtoValidator()
    {
        Include(new AssignmentNoteCreateDtoValidator());
    }
}

public sealed class AssignmentNoteListRequestDtoValidator : CatalogListRequestValidator<AssignmentNoteListRequestDto>
{
    public AssignmentNoteListRequestDtoValidator()
    {
        RuleFor(x => x.NoteCategoryId).GreaterThan(0).When(x => x.NoteCategoryId.HasValue)
            .WithMessage("شناسه موضوع یادداشت باید بزرگتر از صفر باشد.");
        RuleFor(x => x.Search).MaximumLength(AssignmentNoteConstraints.TextMaxLength)
            .WithMessage("عبارت جستجو نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد.");
        RuleFor(x => x.SortBy).Must(x => string.IsNullOrWhiteSpace(x) || new[] { "id", "createddatetime", "modifieddatetime", "seendatetime" }.Contains(x.ToLowerInvariant()))
            .WithMessage("ستون مرتب‌سازی یادداشت معتبر نیست.");
    }
}
