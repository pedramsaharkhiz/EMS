using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence
{
    public class EMSDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationRoleClaim, ApplicationUserToken>
    {
        private readonly AuditSaveChangesInterceptor _auditableInterceptor;

        public EMSDbContext(
            DbContextOptions<EMSDbContext> options,
            AuditSaveChangesInterceptor auditableInterceptor)
            : base(options)
        {
            _auditableInterceptor = auditableInterceptor;
        }
        public DbSet<Province> Provinces => Set<Province>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<SupportingDepartment> SupportingDepartments => Set<SupportingDepartment>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<TechnicianSkill> TechnicianSkills => Set<TechnicianSkill>();
        public DbSet<UserSupportingDepartment> UserSupportingDepartments => Set<UserSupportingDepartment>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<OrganizationManagement> OrganizationManagements => Set<OrganizationManagement>();
        public DbSet<Field> Fields => Set<Field>();
        public DbSet<OrganizationType> OrganizationTypes => Set<OrganizationType>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationStructure> OrganizationStructures => Set<OrganizationStructure>();
        public DbSet<Employee> Employees => Set<Employee>();

        public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();
        public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<EquipmentModel> EquipmentModels => Set<EquipmentModel>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<EquipmentStatus> EquipmentStatuses => Set<EquipmentStatus>();
        public DbSet<Equipment> Equipments => Set<Equipment>();
        public DbSet<EquipmentLocationHistory> EquipmentLocationHistories => Set<EquipmentLocationHistory>();

        public DbSet<FailureCategory> FailureCategories => Set<FailureCategory>();
        public DbSet<FailureType> FailureTypes => Set<FailureType>();
        public DbSet<ReportType> ReportTypes => Set<ReportType>();
        public DbSet<FailureReportStatus> FailureReportStatuses => Set<FailureReportStatus>();
        public DbSet<FailureReport> FailureReports => Set<FailureReport>();
        public DbSet<TechnicianAssignmentStatus> TechnicianAssignmentStatuses => Set<TechnicianAssignmentStatus>();
        public DbSet<TechnicianAssignment> TechnicianAssignments => Set<TechnicianAssignment>();
        public DbSet<ActionEntity> ActionEntities => Set<ActionEntity>();
        public DbSet<TechnicianAssignmentAction> TechnicianAssignmentActions => Set<TechnicianAssignmentAction>();

        public DbSet<Table> Tables => Set<Table>();
        public DbSet<EntityAttachment> EntityAttachments => Set<EntityAttachment>();
        public DbSet<AttachmentContent> AttachmentContents => Set<AttachmentContent>();
        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();




        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            // Dates are stored as UTC; mark them as UTC when read so the API serializes them with a "Z".
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
            // Columns shared by every BaseEntity are configured once (an explicit entity configuration still wins).
            configurationBuilder.Conventions.Add(_ => new Conventions.BaseEntityConvention());
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(EMSDbContext).Assembly);
        }
    }
}