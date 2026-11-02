using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// Guards the database schema: the EF model (entities + configurations + conventions) must produce exactly the
    /// schema recorded by the migrations. A refactoring of the model must not silently change the schema, and a change
    /// of the model must come with a migration (the same check as <c>dotnet ef migrations has-pending-model-changes</c>).
    /// </summary>
    public class SchemaDriftTests
    {
        [Fact]
        public void The_ef_model_produces_exactly_the_schema_of_the_latest_migration()
        {
            using var db = TestDb.SqlServerModelOnly();

            var migrations = db.GetService<IMigrationsAssembly>();
            var snapshotModel = migrations.ModelSnapshot?.Model;
            Assert.NotNull(snapshotModel);

            if (snapshotModel is IMutableModel mutable)
                snapshotModel = mutable.FinalizeModel();
            snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel!);

            var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(
                snapshotModel.GetRelationalModel(),
                db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

            Assert.True(differences.Count == 0,
                "The model differs from the migrations: " + string.Join("; ", differences.Select(d => d.GetType().Name + " " + Describe(d))));
        }

        private static string Describe(MigrationOperation operation) => operation switch
        {
            AlterColumnOperation c => $"{c.Table}.{c.Name}",
            AddColumnOperation c => $"{c.Table}.{c.Name}",
            DropColumnOperation c => $"{c.Table}.{c.Name}",
            CreateTableOperation t => t.Name,
            DropTableOperation t => t.Name,
            _ => operation.ToString() ?? string.Empty
        };
    }
}
