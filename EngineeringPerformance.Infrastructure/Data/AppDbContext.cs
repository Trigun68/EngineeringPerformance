using EngineeringPerformance.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAnalysisVersion> ProjectAnalysisVersions => Set<ProjectAnalysisVersion>();
    public DbSet<PhysicalVolumeType> PhysicalVolumeTypes => Set<PhysicalVolumeType>();
    public DbSet<PhysicalVolumeKfo> PhysicalVolumesKfo => Set<PhysicalVolumeKfo>();
    public DbSet<PhysicalVolumeBer> PhysicalVolumesBer => Set<PhysicalVolumeBer>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<IicWorkDirection> IicWorkDirections => Set<IicWorkDirection>();
    public DbSet<IicWorkDirectionNorm> IicWorkDirectionNorms => Set<IicWorkDirectionNorm>();
    public DbSet<UnitOfMeasurement> UnitsOfMeasurement => Set<UnitOfMeasurement>();
    public DbSet<ProjectPhysicalVolume> ProjectPhysicalVolumes => Set<ProjectPhysicalVolume>();
    public DbSet<ProjectEquipment> ProjectEquipments => Set<ProjectEquipment>();
    public DbSet<ProjectLaborIntensity> ProjectLaborIntensities => Set<ProjectLaborIntensity>();
    public DbSet<ProjectPermission> ProjectPermissions => Set<ProjectPermission>();
    public DbSet<AccessRight> AccessRights => Set<AccessRight>();
    public DbSet<DataImportIteration> DataImportIterations => Set<DataImportIteration>();
    public DbSet<LaborIntensity> LaborIntensities => Set<LaborIntensity>();
    public DbSet<ProjectCharacteristic> ProjectCharacteristics => Set<ProjectCharacteristic>();
    public DbSet<ProjectCharacteristicOption> ProjectCharacteristicOptions => Set<ProjectCharacteristicOption>();
    public DbSet<ProjectCharacteristicSelection> ProjectCharacteristicSelections => Set<ProjectCharacteristicSelection>();
    public DbSet<AnalysisVersionSnapshot> AnalysisVersionSnapshots => Set<AnalysisVersionSnapshot>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<UserActionLog> UserActionLogs => Set<UserActionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectAnalysisVersionConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeKfoConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeBerConfiguration());
        modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new IicWorkDirectionConfiguration());
        modelBuilder.ApplyConfiguration(new IicWorkDirectionNormConfiguration());
        modelBuilder.ApplyConfiguration(new UnitOfMeasurementConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectPhysicalVolumeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectEquipmentConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectLaborIntensityConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectPermissionConfiguration());
        modelBuilder.ApplyConfiguration(new AccessRightConfiguration());
        modelBuilder.ApplyConfiguration(new DataImportIterationConfiguration());
        modelBuilder.ApplyConfiguration(new LaborIntensityConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectCharacteristicConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectCharacteristicOptionConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectCharacteristicSelectionConfiguration());
        modelBuilder.ApplyConfiguration(new AnalysisVersionSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new UserSettingConfiguration());
        modelBuilder.ApplyConfiguration(new UserActionLogConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
