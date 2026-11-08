namespace EMS.Application.Services.Equipment
{
    /// <summary>The location an equipment is at (or moves to).</summary>
    public sealed record EquipmentLocation(long? OrganizationId, long? SupportingDepartmentId, long? TechnicianUserId)
    {
        /// <summary>The message when an equipment is not with exactly one of a branch and a supporting department (the same text as the database constraint's validation).</summary>
        public const string HolderMessage = "تجهیز باید دقیقاً یکی از «شعبه» یا «دپارتمان پشتیبانی (ستاد)» را داشته باشد؛ نه هر دو و نه هیچ‌کدام.";

        /// <summary>True when exactly one of the branch and the supporting department is set.</summary>
        public bool HasExactlyOneHolder => OrganizationId.HasValue ^ SupportingDepartmentId.HasValue;
    }

    /// <summary>
    /// The business rule that says which combinations of organization, department and technician are valid
    /// (e.g. "at a technician" requires a technician). "Exactly one of branch and supporting department" is NOT here: it is a database
    /// constraint and is always checked. The decision is still open with the business, so the registered rule
    /// (<see cref="PermissiveEquipmentLocationRule"/>) accepts everything. When it is decided, write the rule here and register
    /// it instead; creating and moving an equipment already call it, and nothing else has to change.
    /// Throw <c>BadRequestException</c> to refuse.
    /// </summary>
    public interface IEquipmentLocationRule
    {
        Task ValidateAsync(EquipmentLocation location, CancellationToken cancellationToken);
    }

    public sealed class PermissiveEquipmentLocationRule : IEquipmentLocationRule
    {
        public Task ValidateAsync(EquipmentLocation location, CancellationToken cancellationToken) => Task.CompletedTask;
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
