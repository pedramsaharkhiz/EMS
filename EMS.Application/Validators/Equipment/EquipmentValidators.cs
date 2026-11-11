using EMS.Application.Common.Models;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Validators.Catalog;
using EMS.Domain;
using EMS.Application.Services.Equipment;
using FluentValidation;

namespace EMS.Application.Validators.Equipment
{
    internal static class EquipmentRules
    {
        public const string Label = "تجهیز";

        public static IRuleBuilderOptions<T, string?> TextMax<T>(this IRuleBuilder<T, string?> rule, int max, string name) =>
            rule.MaximumLength(max).WithMessage($"{name} {Label} نمی‌تواند بیشتر از {max} کاراکتر باشد.");

        public static IRuleBuilderOptions<T, long?> PositiveId<T>(this IRuleBuilder<T, long?> rule, string name) =>
            rule.Must(id => !id.HasValue || id.Value > 0).WithMessage($"شناسه {name} باید بزرگتر از صفر باشد.");
    }

    public sealed class CreateEquipmentDtoValidator : AbstractValidator<EquipmentCreateDto>
    {
        public CreateEquipmentDtoValidator()
        {
            RuleFor(x => x.Owner)
                .IsInEnum().WithMessage("مالک تجهیز باید شرکت یا بانک باشد.");

            RuleFor(x => x.YaasCode)
                .NotEmpty().WithMessage($"یاس کد {EquipmentRules.Label} الزامی است.")
                .MaximumLength(EquipmentConstraints.YaasCodeMaxLength).WithMessage($"یاس کد {EquipmentRules.Label} نمی‌تواند بیشتر از {EquipmentConstraints.YaasCodeMaxLength} کاراکتر باشد.");
            RuleFor(x => x.SerialNumber).TextMax(EquipmentConstraints.SerialNumberMaxLength, "سریال نامبر");
            RuleFor(x => x.AssetNumber).TextMax(EquipmentConstraints.AssetNumberMaxLength, "شماره دارایی");
            RuleFor(x => x.Description).TextMax(EquipmentConstraints.DescriptionMaxLength, "توضیحات");

            RuleFor(x => x.EquipmentTypeId).PositiveId("نوع تجهیز");
            RuleFor(x => x.EquipmentModelId).PositiveId("مدل تجهیز");
            RuleFor(x => x).Must(x => x.EquipmentTypeId.HasValue ^ x.EquipmentModelId.HasValue)
                .WithName("equipmentTypeId")
                .WithMessage("دقیقاً یکی از نوع تجهیز یا مدل تجهیز باید مقدار داشته باشد.");

            RuleFor(x => x.EquipmentStatusId).GreaterThan(0).WithMessage("شناسه وضعیت تجهیز باید بزرگتر از صفر باشد.");
            RuleFor(x => x.CurrentOrganizationId).PositiveId("شعبه");
            RuleFor(x => x.CurrentSupportingDepartmentId).PositiveId("دپارتمان پشتیبانی");
            RuleFor(x => x).Must(x => x.CurrentOrganizationId.HasValue ^ x.CurrentSupportingDepartmentId.HasValue)
                .WithName("currentOrganizationId")
                .WithMessage(EquipmentLocation.HolderMessage);
            RuleFor(x => x.TechnicianUserId).PositiveId("تکنسین");
            RuleFor(x => x).Must(x => !x.TechnicianUserId.HasValue || x.CurrentSupportingDepartmentId.HasValue)
                .WithName("technicianUserId").WithMessage(EquipmentLocation.TechnicianMessage);
        }
    }

    public sealed class BulkCreateEquipmentDtoValidator : AbstractValidator<EquipmentBulkCreateDto>
    {
        public BulkCreateEquipmentDtoValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("فهرست تجهیزات الزامی است و نباید خالی باشد.")
                .Must(items => items is null || items.Count <= EquipmentConstraints.BulkCreateMaxItems)
                .WithMessage($"در هر درخواست حداکثر {EquipmentConstraints.BulkCreateMaxItems} تجهیز مجاز است.");

            RuleForEach(x => x.Items)
                .NotNull().WithMessage("ردیف تجهیز نمی‌تواند خالی باشد.")
                .SetValidator(new CreateEquipmentDtoValidator()!);

            // The same serial number or asset number twice in one request (they are unique): the later rows are refused. The Yaas code may repeat.
            RuleFor(x => x.Items).Custom((items, context) =>
            {
                if (items is null) return;
                FlagRepeats(items, context, i => i?.SerialNumber, "SerialNumber", "سریال نامبر در این درخواست تکراری است.");
                FlagRepeats(items, context, i => i?.AssetNumber, "AssetNumber", "شماره دارایی در این درخواست تکراری است.");
            });
        }

        private static void FlagRepeats(List<EquipmentCreateDto> items, ValidationContext<EquipmentBulkCreateDto> context, Func<EquipmentCreateDto?, string?> value, string field, string message)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < items.Count; i++)
            {
                var text = value(items[i])?.Trim();
                if (!string.IsNullOrEmpty(text) && !seen.Add(text))
                    context.AddFailure($"Items[{i}].{field}", message);
            }
        }
    }

    public sealed class UpdateEquipmentDtoValidator : AbstractValidator<EquipmentUpdateDto>
    {
        public UpdateEquipmentDtoValidator()
        {
            RuleFor(x => x.Owner)
                .IsInEnum().WithMessage("مالک تجهیز باید شرکت یا بانک باشد.");

            RuleFor(x => x.YaasCode)
                .NotEmpty().WithMessage($"یاس کد {EquipmentRules.Label} الزامی است.")
                .MaximumLength(EquipmentConstraints.YaasCodeMaxLength).WithMessage($"یاس کد {EquipmentRules.Label} نمی‌تواند بیشتر از {EquipmentConstraints.YaasCodeMaxLength} کاراکتر باشد.");
            RuleFor(x => x.SerialNumber).TextMax(EquipmentConstraints.SerialNumberMaxLength, "سریال نامبر");
            RuleFor(x => x.AssetNumber).TextMax(EquipmentConstraints.AssetNumberMaxLength, "شماره دارایی");
            RuleFor(x => x.Description).TextMax(EquipmentConstraints.DescriptionMaxLength, "توضیحات");

            RuleFor(x => x.EquipmentTypeId).PositiveId("نوع تجهیز");
            RuleFor(x => x.EquipmentModelId).PositiveId("مدل تجهیز");
            RuleFor(x => x).Must(x => x.EquipmentTypeId.HasValue ^ x.EquipmentModelId.HasValue)
                .WithName("equipmentTypeId")
                .WithMessage("دقیقاً یکی از نوع تجهیز یا مدل تجهیز باید مقدار داشته باشد.");
            RuleFor(x => x.EquipmentStatusId).GreaterThan(0).WithMessage("شناسه وضعیت تجهیز باید بزرگتر از صفر باشد.");
        }
    }

    public sealed class EquipmentMoveDtoValidator : AbstractValidator<EquipmentMoveDto>
    {
        public EquipmentMoveDtoValidator()
        {
            RuleFor(x => x.CurrentOrganizationId).PositiveId("شعبه");
            RuleFor(x => x.CurrentSupportingDepartmentId).PositiveId("دپارتمان پشتیبانی");
            RuleFor(x => x.TechnicianUserId).PositiveId("تکنسین");
            RuleFor(x => x.Description).TextMax(EquipmentConstraints.DescriptionMaxLength, "توضیحات");

            // The equipment is with a branch OR a supporting department: one move names at most one of them.
            RuleFor(x => x).Must(x => !(x.CurrentOrganizationId.HasValue && x.CurrentSupportingDepartmentId.HasValue))
                .WithName("currentOrganizationId")
                .WithMessage(EquipmentLocation.MoveBothMessage);
            RuleFor(x => x).Must(x => !(x.ClearTechnician && x.TechnicianUserId.HasValue))
                .WithName("clearTechnician")
                .WithMessage(EquipmentLocation.MoveClearAndSetTechnicianMessage);
            // a move that asks for nothing is a mistake of the caller, not a silent success
            RuleFor(x => x).Must(x => x.CurrentOrganizationId.HasValue || x.CurrentSupportingDepartmentId.HasValue || x.TechnicianUserId.HasValue || x.ClearTechnician)
                .WithName("technicianUserId")
                .WithMessage(EquipmentLocation.MoveNothingMessage);
        }
    }

    public sealed class EquipmentBulkMoveDtoValidator : AbstractValidator<EquipmentBulkMoveDto>
    {
        public EquipmentBulkMoveDtoValidator()
        {
            RuleFor(x => x.EquipmentIds)
                .NotEmpty().WithMessage("فهرست تجهیزات الزامی است و نباید خالی باشد.")
                .Must(ids => ids is null || ids.Count <= EquipmentConstraints.BulkMoveMaxItems)
                .WithMessage($"در هر درخواست حداکثر {EquipmentConstraints.BulkMoveMaxItems} تجهیز مجاز است.");
            RuleForEach(x => x.EquipmentIds).GreaterThan(0).WithMessage("شناسه تجهیز باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentIds)
                .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
                .WithMessage("یک تجهیز نباید بیش از یک بار در فهرست بیاید.");

            // the change is the one of a single move
            RuleFor(x => x.CurrentOrganizationId).PositiveId("شعبه");
            RuleFor(x => x.CurrentSupportingDepartmentId).PositiveId("دپارتمان پشتیبانی");
            RuleFor(x => x.TechnicianUserId).PositiveId("تکنسین");
            RuleFor(x => x.Description).TextMax(EquipmentConstraints.DescriptionMaxLength, "توضیحات");
            RuleFor(x => x).Must(x => !(x.CurrentOrganizationId.HasValue && x.CurrentSupportingDepartmentId.HasValue))
                .WithName("currentOrganizationId").WithMessage(EquipmentLocation.MoveBothMessage);
            RuleFor(x => x).Must(x => !(x.ClearTechnician && x.TechnicianUserId.HasValue))
                .WithName("clearTechnician").WithMessage(EquipmentLocation.MoveClearAndSetTechnicianMessage);
            RuleFor(x => x).Must(x => x.CurrentOrganizationId.HasValue || x.CurrentSupportingDepartmentId.HasValue || x.TechnicianUserId.HasValue || x.ClearTechnician)
                .WithName("technicianUserId").WithMessage(EquipmentLocation.MoveNothingMessage);
        }
    }

    public sealed class EquipmentListRequestDtoValidator : CatalogListRequestValidator<EquipmentListRequestDto>
    {
        private const int MaxValues = 200;

        public EquipmentListRequestDtoValidator()
        {
            RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.YaasCode).MaximumLength(100).WithMessage("یاس کد جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.SerialNumber).MaximumLength(100).WithMessage("سریال نامبر جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.AssetNumber).MaximumLength(100).WithMessage("شماره دارایی جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");

            RuleFor(x => x.Owners)
                .Must(values => values is null || values.Length <= MaxValues)
                .WithMessage($"حداکثر {MaxValues} مقدار برای فیلتر مالک مجاز است.");
            RuleForEach(x => x.Owners)
                .IsInEnum().WithMessage("مالک تجهیز باید شرکت یا بانک باشد.");

            Ids(x => x.StatusIds, "وضعیت");
            Ids(x => x.CategoryIds, "دسته‌بندی");
            Ids(x => x.TypeIds, "نوع تجهیز");
            Ids(x => x.ModelIds, "مدل");
            Ids(x => x.BrandIds, "برند");
            RuleFor(x => x.LocationKinds)
                .Must(values => values is null || values.Length <= MaxValues)
                .WithMessage($"حداکثر {MaxValues} مقدار برای فیلتر محل نگهداری مجاز است.");
            RuleForEach(x => x.LocationKinds)
                .IsInEnum().WithMessage("محل نگهداری باید شعبه یا ستاد باشد.");
            Ids(x => x.OrganizationIds, "شعبه");
            Ids(x => x.FieldIds, "حوزه");
            Ids(x => x.OrganizationManagementIds, "مدیریت شعب");
            Ids(x => x.ZoneIds, "ناحیه");
            Ids(x => x.SupportingDepartmentIds, "دپارتمان پشتیبانی شعبه");
            Ids(x => x.CurrentSupportingDepartmentIds, "دپارتمان پشتیبانی تجهیز");
            Ids(x => x.CityIds, "شهر");
            Ids(x => x.ProvinceIds, "استان");
            Ids(x => x.TechnicianUserIds, "تکنسین");

            RuleFor(x => x.CreatedTo).GreaterThanOrEqualTo(x => x.CreatedFrom!.Value)
                .When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue)
                .WithMessage("پایان بازه‌ی تاریخ ایجاد نمی‌تواند قبل از شروع آن باشد.");
        }

        private void Ids(System.Linq.Expressions.Expression<Func<EquipmentListRequestDto, long[]?>> ids, string name)
        {
            RuleFor(ids).Must(v => v is null || v.Length <= MaxValues)
                .WithMessage($"حداکثر {MaxValues} مقدار برای فیلتر {name} مجاز است.");
            RuleFor(ids).Must(v => v is null || v.All(id => id > 0))
                .WithMessage($"شناسه {name} باید بزرگتر از صفر باشد.");
        }
    }

        /// <summary>Validates organization equipment filters, paging and sorting.</summary>
        public sealed class OrganizationEquipmentListRequestDtoValidator : CatalogListRequestValidator<OrganizationEquipmentListRequestDto>
        {
            public OrganizationEquipmentListRequestDtoValidator()
            {
                RuleFor(x => x.OrganizationId)
                    .GreaterThan(0)
                    .WithMessage("شناسه سازمان باید بزرگ‌تر از صفر باشد.");

                RuleFor(x => x.Search)
                    .MaximumLength(100)
                    .WithMessage("عبارت جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");

            }

            /// <summary>Allows sorting by the fields exposed in the response.</summary>
            private static bool IsSupportedSort(string? sortBy)
            {
                if (string.IsNullOrWhiteSpace(sortBy))
                    return true;

                return sortBy.Equals("id", StringComparison.OrdinalIgnoreCase)
                    || sortBy.Equals("yaasCode", StringComparison.OrdinalIgnoreCase)
                    || sortBy.Equals("serialNumber", StringComparison.OrdinalIgnoreCase)
                    || sortBy.Equals("assetNumber", StringComparison.OrdinalIgnoreCase);
            }
        }


    public sealed class TechnicianListRequestDtoValidator : CatalogListRequestValidator<TechnicianListRequestDto>
    {
        public TechnicianListRequestDtoValidator()
        {
            RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جست‌وجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
        }
    }

    public sealed class EquipmentLocationHistoryRequestDtoValidator : CatalogListRequestValidator<EMS.Application.Dtos.EquipmentLocationHistory.EquipmentLocationHistoryRequestDto>
    {
    }
}
