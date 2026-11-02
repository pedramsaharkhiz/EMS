namespace EMS.Domain
{
    /// <summary>
    /// Length limits of the columns every catalog entity has. The database column size (EF configuration) and the
    /// request validation (validators) both use these constants, so they can never disagree
    /// (a validator that allows more than the column holds turns bad input into a 500).
    /// </summary>
    public static class CatalogConstraints
    {
        public const int TitleMaxLength = 100;
        public const int CodeMaxLength = 20;
    }
}
