namespace EMS.Domain
{
    /// <summary>
    /// Marks an entity that can have attachments. At startup the Tables registry (and its IsAttachmentEnabled flag) is
    /// filled from every entity that carries this attribute, so making a new entity attachable is one attribute and nothing else.
    /// The attachment endpoints authorize with the entity's own permissions: <c>{PermissionGroup}.View</c> to list and download,
    /// <c>{PermissionGroup}.Update</c> to upload and deactivate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class AttachableAttribute : Attribute
    {
        public AttachableAttribute(string displayName, string permissionGroup)
        {
            DisplayName = displayName;
            PermissionGroup = permissionGroup;
        }

        /// <summary>Persian name shown to users (Tables.DisplayName).</summary>
        public string DisplayName { get; }

        /// <summary>The permission group of the entity, e.g. "Equipments" (Permissions.Equipments).</summary>
        public string PermissionGroup { get; }

        public string ViewPermission => $"{PermissionGroup}.View";
        public string EditPermission => $"{PermissionGroup}.Update";
    }
}
