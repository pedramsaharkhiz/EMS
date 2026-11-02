using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;
using EMS.Domain;
using FluentValidation;
using System.Linq.Expressions;

namespace EMS.Application.Validators.Catalog
{
    /// <summary>
    /// The validation every catalog's list request shares: paging bounds and sort direction.
    /// A catalog's validator is one line (<c>: CatalogListRequestValidator&lt;MyListRequestDto&gt;</c>);
    /// add rules for its own filters in its constructor.
    /// </summary>
    public abstract class CatalogListRequestValidator<T> : AbstractValidator<T> where T : PagedRequest
    {
        protected CatalogListRequestValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل 1 باشد.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین 1 تا 100 باشد.");
            RuleFor(x => x.SortDirection)
                .Must(x => string.IsNullOrWhiteSpace(x) || x.Equals("asc", StringComparison.OrdinalIgnoreCase) || x.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("جهت مرتب‌سازی باید asc یا desc باشد.");
        }
    }

    /// <summary>
    /// Create-request rules every catalog shares: a Title that is present (blank counts as missing) and fits the column,
    /// and, when the catalog has one, a Code that fits its column. Add rules for parents etc. in the constructor.
    /// </summary>
    public abstract class CatalogCreateValidator<T> : AbstractValidator<T> where T : ICatalogCreateRequest
    {
        /// <param name="label">Persian name of the catalog used in messages.</param>
        /// <param name="code">The Code property (e.g. <c>x =&gt; x.Code</c>), or null when the catalog has no code.</param>
        protected CatalogCreateValidator(string label, Expression<Func<T, string?>>? code = null)
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage($"عنوان {label} الزامی است.")
                .MaximumLength(CatalogConstraints.TitleMaxLength)
                .WithMessage($"عنوان {label} نمی‌تواند بیشتر از {CatalogConstraints.TitleMaxLength} کاراکتر باشد.");

            if (code is not null)
            {
                RuleFor(code)
                    .MaximumLength(CatalogConstraints.CodeMaxLength)
                    .WithMessage($"کد {label} نمی‌تواند بیشتر از {CatalogConstraints.CodeMaxLength} کاراکتر باشد.");
            }
        }
    }

    /// <summary>
    /// Update-request rules every catalog shares: a positive id, a Title (if sent) and Code (if the catalog has one)
    /// that fit their columns.
    /// </summary>
    public abstract class CatalogUpdateValidator<T> : AbstractValidator<T> where T : ICatalogUpdateRequest
    {
        /// <param name="label">Persian name of the catalog used in messages.</param>
        /// <param name="id">The id property (e.g. <c>x =&gt; x.BrandId</c>); its name appears in the error response.</param>
        /// <param name="code">The Code property, or null when the catalog has no code.</param>
        protected CatalogUpdateValidator(string label, Expression<Func<T, long>> id, Expression<Func<T, string?>>? code = null)
        {
            RuleFor(id).GreaterThan(0).WithMessage($"شناسه {label} باید بزرگتر از صفر باشد.");

            RuleFor(x => x.Title)
                .MaximumLength(CatalogConstraints.TitleMaxLength)
                .WithMessage($"عنوان {label} نمی‌تواند بیشتر از {CatalogConstraints.TitleMaxLength} کاراکتر باشد.");

            if (code is not null)
            {
                RuleFor(code)
                    .MaximumLength(CatalogConstraints.CodeMaxLength)
                    .WithMessage($"کد {label} نمی‌تواند بیشتر از {CatalogConstraints.CodeMaxLength} کاراکتر باشد.");
            }
        }
    }
}
