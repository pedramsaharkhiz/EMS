using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Interfaces.Attachment;
using EMS.Application.Services.Attachment;
using EMS.Application.Services.Equipment;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Persistence.Seed;
using EMS.Infrastructure.Repositories;
using EMS.Infrastructure.Services.Attachments;
using EMS.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// "All or nothing" of registering an equipment together with its files, against a real SQL Server: the equipment, its location history
    /// and every file are one transaction, so a refused file leaves nothing behind. The in-memory database ignores transactions, so the
    /// API tests (EquipmentWithAttachmentsTests) cannot prove this. Manual: skipped unless EMS_TEST_SQL is set (see SqlServerFactAttribute);
    /// it drops and recreates a throwaway database whose name must contain "test".
    /// </summary>
    public class SqlEquipmentRegistrationTests
    {
        private sealed class FakeCurrentUser : EMS.Application.Common.Interfaces.ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "test";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private sealed class NoAudit : IAuditEventLogger
        {
            public int AttachmentAudits;
            public int EquipmentAudits;
            public void Created(string entityType, object? entityId)
            {
                if (entityType == "EntityAttachment") AttachmentAudits++;
                if (entityType == "Equipment") EquipmentAudits++;
            }
            public void Updated(string entityType, object? entityId) { }
            public void Deleted(string entityType, object? entityId) { }
            public void Operation(string operation, string entityType, object? entityId, string? detail = null) { }
        }

        private static EMSDbContext NewContext()
        {
            var builder = new SqlConnectionStringBuilder(SqlServerFactAttribute.ConnectionString);
            Assert.Contains("test", builder.InitialCatalog, StringComparison.OrdinalIgnoreCase);
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer(builder.ConnectionString).Options;
            return new EMSDbContext(options, new AuditSaveChangesInterceptor(new FakeCurrentUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(), Microsoft.Extensions.Options.Options.Create(new EMS.Infrastructure.Options.AuditTrailOptions()), NullLogger<AuditSaveChangesInterceptor>.Instance));
        }

        private static (EquipmentService Service, NoAudit Audit) NewService(EMSDbContext context)
        {
            var audit = new NoAudit();
            var unitOfWork = new UnitOfWork(context);
            var attachments = new AttachmentService(
                new EntityAttachmentRepository(context), new Repository<AttachmentContent>(context), new AttachmentTargetCatalog(context),
                new SqlAttachmentBlobStore(context), unitOfWork, audit, new UserReferenceRepository(context));
            var service = new EquipmentService(
                new EquipmentRepository(context), new EquipmentStatusRepository(context), new EquipmentTypeRepository(context), new EquipmentModelRepository(context),
                new EquipmentLocationTypeRepository(context), new SupportingDepartmentRepository(context), new Repository<Organization>(context),
                new PermissiveEquipmentLocationRule(), exporter: null!, new FakeCurrentUser(), unitOfWork, audit, attachments, new UserReferenceRepository(context));
            return (service, audit);
        }

        private static async Task<(EMSDbContext Context, EquipmentCreateDto Request)> NewDatabaseAsync(string yaas)
        {
            var db = NewContext();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();                         // also inserts the seeded statuses (InStock = 1)
            await new AttachmentTableSeeder(db, NullLogger<AttachmentTableSeeder>.Instance).SeedAsync();

            db.Users.Add(new ApplicationUser { UserName = "tester", NormalizedUserName = "TESTER", FirstName = "t", LastName = "t", PersonnelCode = "T1", CreatedByUserId = 0 });
            var category = new EquipmentCategory { Title = "c" };
            var type = new EquipmentType { Title = "t", EquipmentCategory = category };
            var location = new EquipmentLocationType { Title = "l" };
            db.AddRange(category, type, location);
            await db.SaveChangesAsync();

            return (db, new EquipmentCreateDto
            {
                YaasCode = yaas, Owner = EquipmentOwner.Company, EquipmentTypeId = type.EquipmentTypeId,
                EquipmentStatusId = (long)EquipmentStatusCode.InStock, CurrentEquipmentLocationTypeId = location.EquipmentLocationTypeId,
            });
        }

        private static byte[] Pdf(int size = 3000) => Encoding.ASCII.GetBytes("%PDF-1.7\n").Concat(new byte[size]).ToArray();

        private static async IAsyncEnumerable<AttachmentUpload> Files(params (string Name, byte[] Bytes)[] files)
        {
            foreach (var (name, bytes) in files)
            {
                yield return new AttachmentUpload(name, new MemoryStream(bytes), null);
                await Task.CompletedTask;
            }
        }

        [SqlServerFact]
        public async Task An_equipment_and_its_files_are_saved_together()
        {
            var (db, request) = await NewDatabaseAsync("Y-OK");
            await using var _ = db;
            var (service, audit) = NewService(db);

            var result = await service.CreateWithAttachmentsAsync(request, Files(("a.pdf", Pdf()), ("b.pdf", Pdf(5000))));

            Assert.Equal(2, result.Attachments.Count);
            Assert.Equal(1, await db.Equipments.CountAsync(e => e.YaasCode == "Y-OK"));
            Assert.Equal(2, await db.EntityAttachments.CountAsync(a => a.EntityId == result.Id));
            Assert.Equal(2, await db.AttachmentContents.CountAsync());
            Assert.Equal(1, await db.EquipmentLocationHistories.CountAsync(h => h.EquipmentId == result.Id));
            Assert.Equal((1, 2), (audit.EquipmentAudits, audit.AttachmentAudits));
        }

        [SqlServerFact]
        public async Task A_refused_file_leaves_no_equipment_no_history_no_file_and_no_audit_entry()
        {
            var (db, request) = await NewDatabaseAsync("Y-REFUSED");
            await using var _ = db;
            var (service, audit) = NewService(db);

            // the first file is fine and is already stored inside the transaction when the second one is refused
            var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
                service.CreateWithAttachmentsAsync(request, Files(("good.pdf", Pdf()), ("virus.exe", Pdf()))));
            Assert.Equal(ErrorCodes.AttachmentTypeNotAllowed, ex.Code);

            db.ChangeTracker.Clear();
            Assert.False(await db.Equipments.IgnoreQueryFilters().AnyAsync(e => e.YaasCode == "Y-REFUSED"));
            Assert.Equal(0, await db.EquipmentLocationHistories.CountAsync());
            Assert.Equal(0, await db.EntityAttachments.CountAsync());
            Assert.Equal(0, await db.AttachmentContents.CountAsync());
            Assert.Equal((0, 0), (audit.EquipmentAudits, audit.AttachmentAudits));

            // and the same Yaas code can be registered afterwards: nothing is holding it
            var again = await service.CreateWithAttachmentsAsync(request, Files(("good.pdf", Pdf())));
            Assert.Single(again.Attachments);
        }

        [SqlServerFact]
        public async Task A_sixth_file_rolls_everything_back()
        {
            var (db, request) = await NewDatabaseAsync("Y-SIX");
            await using var _ = db;
            var (service, _) = NewService(db);

            var six = Enumerable.Range(0, AttachmentRules.MaxFilesPerRegistration + 1).Select(i => ($"f{i}.pdf", Pdf())).ToArray();
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateWithAttachmentsAsync(request, Files(six)));
            Assert.Equal(ErrorCodes.AttachmentsPerRequestExceeded, ex.Code);

            db.ChangeTracker.Clear();
            Assert.False(await db.Equipments.IgnoreQueryFilters().AnyAsync(e => e.YaasCode == "Y-SIX"));
            Assert.Equal(0, await db.AttachmentContents.CountAsync());
        }
    }
}
