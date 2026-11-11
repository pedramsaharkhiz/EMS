using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EMS.Tests.Integration;

// Direct writes deliberately bypass application validation: these tests prove the migrated SQL table enforces the rules.
public class SqlEquipmentLocationConstraintTests
{
    private const string Migration = "20261111093443_SetConstraintForOrganizationSupoortingDepartmentTechnecianInEquipment";

    private static EMSDbContext NewContext()
    {
        var connection = new SqlConnectionStringBuilder(SqlServerFactAttribute.ConnectionString);
        Assert.Contains("test", connection.InitialCatalog, StringComparison.OrdinalIgnoreCase);
        // Every test owns its database, including when the other SQL test classes run in parallel.
        connection.InitialCatalog = "EMS_location_test_" + Guid.NewGuid().ToString("N");
        var interceptor = TestDb.NewInterceptor();
        return new EMSDbContext(new DbContextOptionsBuilder<EMSDbContext>()
            .UseSqlServer(connection.ConnectionString).AddInterceptors(interceptor).Options, interceptor);
    }

    private sealed record References(long Type, long Branch, long Department, long Technician);

    private static async Task<References> SeedAsync(EMSDbContext db)
    {
        var type = new EquipmentType { Title = "نوع آزمایشی", EquipmentCategory = new EquipmentCategory { Title = "دسته آزمایشی" } };
        var branch = new Organization
        {
            Title = "شعبه آزمایشی", OrganizationType = new OrganizationType { Title = "نوع شعبه" },
            OrganizationManagement = new OrganizationManagement { Title = "مدیریت آزمایشی", Zone = new Zone { Title = "ناحیه آزمایشی" } }
        };
        var department = new SupportingDepartment
        {
            Title = "ستاد آزمایشی", City = new City { Title = "شهر آزمایشی", Province = new Province { Title = "استان آزمایشی" } }
        };
        var technician = new ApplicationUser { UserName = "location-test", NormalizedUserName = "LOCATION-TEST", FirstName = "آزمایش", LastName = "تکنسین", PersonnelCode = "LOCATION-TEST" };
        db.AddRange(type, branch, department, technician);
        await db.SaveChangesAsync();
        return new(type.EquipmentTypeId, branch.OrganizationId, department.SupportingDepartmentId, technician.Id);
    }

    private static Equipment Equipment(References ids, bool branch, bool department, bool technician) => new()
    {
        YaasCode = Guid.NewGuid().ToString("N"), Owner = EquipmentOwner.Company,
        EquipmentTypeId = ids.Type, EquipmentStatusId = (long)EquipmentStatusCode.InStock,
        CurrentOrganizationId = branch ? ids.Branch : null,
        CurrentSupportingDepartmentId = department ? ids.Department : null,
        TechnicianUserId = technician ? ids.Technician : null
    };

    private static async Task AssertCheckRejectedAsync(EMSDbContext db, string? constraint = null)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Equal(547, sql.Number);
        if (constraint is not null) Assert.Contains(constraint, sql.Message);
        db.ChangeTracker.Clear();
    }

    [SqlServerFact]
    public async Task Migrated_table_checks_all_eight_insert_combinations_and_updates()
    {
        await using var db = NewContext();
        try
        {
            await db.Database.MigrateAsync();
            var ids = await SeedAsync(db);
            foreach (var branch in new[] { false, true })
            foreach (var department in new[] { false, true })
            foreach (var technician in new[] { false, true })
            {
                var allowed = (branch ^ department) && (!technician || department);
                var before = await db.Equipments.CountAsync();
                db.Add(Equipment(ids, branch, department, technician));
                if (allowed) await db.SaveChangesAsync();
                else await AssertCheckRejectedAsync(db, branch && !department && technician ? EquipmentConstraints.TechnicianCheckName : null);
                Assert.Equal(before + (allowed ? 1 : 0), await db.Equipments.CountAsync());
                db.ChangeTracker.Clear();
            }

            var atBranch = await db.Equipments.SingleAsync(x => x.CurrentOrganizationId != null);
            var branchId = atBranch.EquipmentId;
            atBranch.TechnicianUserId = ids.Technician;
            await AssertCheckRejectedAsync(db, EquipmentConstraints.TechnicianCheckName);
            Assert.Null((await db.Equipments.SingleAsync(x => x.EquipmentId == branchId)).TechnicianUserId);
            db.ChangeTracker.Clear();

            var atDepartment = await db.Equipments.SingleAsync(x => x.TechnicianUserId != null);
            var equipmentId = atDepartment.EquipmentId;
            atDepartment.CurrentOrganizationId = ids.Branch;
            atDepartment.CurrentSupportingDepartmentId = null;
            await AssertCheckRejectedAsync(db, EquipmentConstraints.TechnicianCheckName);
            atDepartment = await db.Equipments.SingleAsync(x => x.EquipmentId == equipmentId);
            Assert.Equal(ids.Department, atDepartment.CurrentSupportingDepartmentId);
            Assert.Equal(ids.Technician, atDepartment.TechnicianUserId);
            atDepartment.CurrentOrganizationId = ids.Branch;
            atDepartment.CurrentSupportingDepartmentId = null;
            atDepartment.TechnicianUserId = null;
            await db.SaveChangesAsync();
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    public async Task Migration_refuses_legacy_invalid_data_until_explicitly_corrected()
    {
        await using var db = NewContext();
        try
        {
            var migrations = db.Database.GetMigrations().ToList();
            var index = migrations.IndexOf(Migration);
            Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var ids = await SeedAsync(db);
            var legacy = Equipment(ids, branch: true, department: false, technician: true);
            db.Add(legacy);
            await db.SaveChangesAsync();

            // The new migration must not silently discard an existing technician.
            var error = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync(Migration));
            Assert.Equal(547, error.Number);
            Assert.Contains(EquipmentConstraints.TechnicianCheckName, error.Message);
            Assert.DoesNotContain(Migration, await db.Database.GetAppliedMigrationsAsync());
            db.ChangeTracker.Clear();
            legacy = await db.Equipments.SingleAsync();
            Assert.Equal(ids.Technician, legacy.TechnicianUserId);
            legacy.TechnicianUserId = null; // An explicit correction, never part of the migration.
            await db.SaveChangesAsync();
            await db.GetService<IMigrator>().MigrateAsync(Migration);
            Assert.Contains(Migration, await db.Database.GetAppliedMigrationsAsync());
            legacy.TechnicianUserId = ids.Technician;
            await AssertCheckRejectedAsync(db, EquipmentConstraints.TechnicianCheckName);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
