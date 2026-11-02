namespace EMS.Application.Common.Logging
{
    /// <summary>
    /// Writes business / administrative operations to the <see cref="LogCategories.Audit"/> category.
    /// Only identifiers and operation names are logged, never field contents.
    /// </summary>
    public interface IAuditEventLogger
    {
        void Created(string entityType, object? entityId);
        void Updated(string entityType, object? entityId);
        void Deleted(string entityType, object? entityId);

        /// <summary>
        /// A named operation that is not a plain create/update/delete,
        /// e.g. "PasswordChanged", "RolesAssigned", "PermissionsChanged".
        /// </summary>
        void Operation(string operation, string entityType, object? entityId, string? detail = null);
    }
}
