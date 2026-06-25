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
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AccessRightConfiguration());
        modelBuilder.ApplyConfiguration(new UserActionLogConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectPhysicalVolumeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectLaborIntensityConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeKfoConfiguration());
        modelBuilder.ApplyConfiguration(new PhysicalVolumeBerConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
