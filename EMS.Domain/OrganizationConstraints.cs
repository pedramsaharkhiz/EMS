namespace EMS.Domain
{
    /// <summary>Database rules of <c>Organization</c> (a Bank Mellat branch), shared by the EF configuration and the tests.</summary>
    public static class OrganizationConstraints
    {
        /// <summary>
        /// A branch hangs under exactly ONE parent: a field (حوزه) or, when it has no field, directly under a branch management
        /// (مدیریت شعبه). Neither and both are refused. See docs/BUSINESS-DECISIONS.md, section 0.
        /// </summary>
        public const string ParentCheckName = "CK_Organizations_FieldOrManagement_ExactlyOne";

        public const string ParentCheckSql =
            "([FieldId] IS NOT NULL AND [OrganizationManagementId] IS NULL) OR ([FieldId] IS NULL AND [OrganizationManagementId] IS NOT NULL)";
    }
}
