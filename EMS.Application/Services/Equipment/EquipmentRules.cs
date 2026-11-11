using EMS.Application.Common.Exceptions;

namespace EMS.Application.Services.Equipment
{
    /// <summary>The location an equipment is at (or moves to).</summary>
    public sealed record EquipmentLocation(long? OrganizationId, long? SupportingDepartmentId, long? TechnicianUserId)
    {
        public const string TechnicianMessage = "تکنسین فقط برای تجهیز نزد دپارتمان پشتیبانی (ستاد) قابل تعیین است؛ تجهیز نزد شعبه نمی‌تواند تکنسین داشته باشد.";

        public const string HolderMessage = "تجهیز باید دقیقاً یکی از «شعبه» یا «دپارتمان پشتیبانی (ستاد)» را داشته باشد؛ نه هر دو و نه هیچ‌کدام.";

        // the messages of the partial move (POST .../move): see EquipmentMoveDto
        public const string MoveBothMessage = "در هر جابه‌جایی فقط یکی از «شعبه» یا «دپارتمان پشتیبانی (ستاد)» را بفرست؛ تجهیز هم‌زمان نزد هر دو نیست.";
        public const string MoveClearAndSetTechnicianMessage = "تکنسین را هم‌زمان نمی‌شود تعیین و برداشت؛ فقط یکی از «تکنسین» یا «برداشتن تکنسین» را بفرست.";
        public const string MoveNothingMessage = "حداقل یکی از «شعبه»، «دپارتمان پشتیبانی (ستاد)»، «تکنسین» یا «برداشتن تکنسین» را بفرست.";

        /// <summary>True when exactly one of the branch and the supporting department is set.</summary>
        public bool HasExactlyOneHolder => OrganizationId.HasValue ^ SupportingDepartmentId.HasValue;
    }

    /// <summary>A technician is optional and allowed only when the equipment is at a supporting department.</summary>
    public interface IEquipmentLocationRule
    {
        Task ValidateAsync(EquipmentLocation location, CancellationToken cancellationToken);
    }

    public sealed class DepartmentTechnicianLocationRule : IEquipmentLocationRule
    {
        public Task ValidateAsync(EquipmentLocation location, CancellationToken cancellationToken)
        {
            if (location.TechnicianUserId.HasValue && !location.SupportingDepartmentId.HasValue)
                throw new BadRequestException(EquipmentLocation.TechnicianMessage,
                    ErrorCodes.TechnicianRequiresDepartment);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Which status changes are allowed. Today every change is (the business wants no restriction). To restrict, return false
    /// here, e.g. <c>from == scrappedId</c>; the update action reports it as <c>400 status_transition_not_allowed</c>. Status ids
    /// are the values of <c>EquipmentStatusCode</c>.
    /// </summary>
    public static class EquipmentStatusTransitions
    {
        public static bool IsAllowed(long fromStatusId, long toStatusId) => true;
    }
}
