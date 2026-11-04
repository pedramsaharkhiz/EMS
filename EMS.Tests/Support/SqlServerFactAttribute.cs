namespace EMS.Tests.Support
{
    /// <summary>
    /// A test that needs a real SQL Server (the in-memory database cannot run raw SQL or streaming). It is skipped unless the environment
    /// variable <c>EMS_TEST_SQL</c> holds a connection string to a THROWAWAY database whose name contains "test": the test drops and
    /// recreates it. Example (PowerShell): <c>$env:EMS_TEST_SQL = "data source=host;initial catalog=EMS_attachments_test;..."</c>.
    /// </summary>
    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public const string Variable = "EMS_TEST_SQL";

        public SqlServerFactAttribute()
        {
            var connection = Environment.GetEnvironmentVariable(Variable);
            if (string.IsNullOrWhiteSpace(connection))
                Skip = $"Needs SQL Server: set {Variable} to a connection string of a throwaway database (its name must contain 'test').";
        }

        public static string ConnectionString => Environment.GetEnvironmentVariable(Variable)!;
    }
}
