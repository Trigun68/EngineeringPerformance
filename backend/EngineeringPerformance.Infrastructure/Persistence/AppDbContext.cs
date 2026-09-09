using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAnalysisVersion> ProjectAnalysisVersions => Set<ProjectAnalysisVersion>();
    public DbSet<PhysicalVolumeType> PhysicalVolumeTypes => Set<PhysicalVolumeType>();
    public DbSet<PhysicalVolumeKfo> PhysicalVolumesKfo => Set<PhysicalVolumeKfo>();
    public DbSet<PhysicalVolumeBer> PhysicalVolumesBer => Set<PhysicalVolumeBer>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<IicWorkDirection> IicWorkDirections => Set<IicWorkDirection>();
    public DbSet<IicWorkDirectionNorm> IicWorkDirectionNorms => Set<IicWorkDirectionNorm>();
    public DbSet<IicWorkDirectionWorkCost> IicWorkDirectionWorkCosts => Set<IicWorkDirectionWorkCost>();
    public DbSet<UnitOfMeasurement> UnitsOfMeasurement => Set<UnitOfMeasurement>();
    public DbSet<DocSet> DocSets => Set<DocSet>();
    public DbSet<VersionCalculationLog> VersionCalculationLogs => Set<VersionCalculationLog>();
    public DbSet<ProjectPhysicalVolume> ProjectPhysicalVolumes => Set<ProjectPhysicalVolume>();
    public DbSet<ProjectEquipment> ProjectEquipments => Set<ProjectEquipment>();
    public DbSet<ProjectLaborIntensity> ProjectLaborIntensities => Set<ProjectLaborIntensity>();
    public DbSet<ProjectPermission> ProjectPermissions => Set<ProjectPermission>();
    public DbSet<AccessRight> AccessRights => Set<AccessRight>();
    public DbSet<DataImportIteration> DataImportIterations => Set<DataImportIteration>();
    public DbSet<LaborIntensity> LaborIntensities => Set<LaborIntensity>();
    public DbSet<LaborIntensityIicBinding> LaborIntensityIicBindings => Set<LaborIntensityIicBinding>();
    public DbSet<ProjectCharacteristic> ProjectCharacteristics => Set<ProjectCharacteristic>();
    public DbSet<ProjectCharacteristicOption> ProjectCharacteristicOptions => Set<ProjectCharacteristicOption>();
    public DbSet<ProjectCharacteristicSelection> ProjectCharacteristicSelections => Set<ProjectCharacteristicSelection>();
    public DbSet<AnalysisVersionSnapshot> AnalysisVersionSnapshots => Set<AnalysisVersionSnapshot>();
    public DbSet<ProjectVersionPlanningParameter> ProjectVersionPlanningParameters => Set<ProjectVersionPlanningParameter>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<UserActionLog> UserActionLogs => Set<UserActionLog>();
    public DbSet<SqlQueryMetric> SqlQueryMetrics => Set<SqlQueryMetric>();
    public DbSet<ResourceMetricSample> ResourceMetricSamples => Set<ResourceMetricSample>();
    public DbSet<NormSuggestion> NormSuggestions => Set<NormSuggestion>();
    public DbSet<NormChangeAudit> NormChangeAudits => Set<NormChangeAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new AuthAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new AccessRightConfiguration());
        modelBuilder.ApplyConfiguration(new UserActionLogConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectPhysicalVolumeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectLaborIntensityConfiguration());
        modelBuilder.ApplyConfiguration(new DocSetConfiguration());
        modelBuilder.ApplyConfiguration(new VersionCalculationLogConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeKfoConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeBerConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectVersionPlanningParameterConfiguration());
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new SqlQueryMetricConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceMetricSampleConfiguration());
        modelBuilder.ApplyConfiguration(new IicWorkDirectionNormConfiguration());
        modelBuilder.ApplyConfiguration(new IicWorkDirectionWorkCostConfiguration());
        modelBuilder.ApplyConfiguration(new LaborIntensityIicBindingConfiguration());
        modelBuilder.ApplyConfiguration(new DataImportIterationConfiguration());
        modelBuilder.ApplyConfiguration(new NormSuggestionConfiguration());
        modelBuilder.ApplyConfiguration(new NormChangeAuditConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectEquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectAnalysisVersionConfiguration());

        var rowVersion = modelBuilder.Entity<ProjectAnalysisVersion>().Property(v => v.RowVersion);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            rowVersion.IsRowVersion();
        }
        else
        {
            // InMemory / тесты: не SQL rowversion, иначе SaveChanges падает без значения.
            rowVersion.IsConcurrencyToken();
            rowVersion.HasDefaultValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });
        }

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entityType => entityType.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(4);
        }

        base.OnModelCreating(modelBuilder);
    }
}
