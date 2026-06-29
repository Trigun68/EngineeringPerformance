using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Deep Copy версии на MS SQL (Testcontainers): PPV-иерархия, LI, итерации, Equipment.
/// </summary>
public sealed class ProjectVersionDeepCopyTests : IAsyncLifetime
{
    private const string MsSqlPassword = "YourStrong@Passw0rd";

    private MsSqlContainer? _msSqlContainer;
    private IDbContextFactory<AppDbContext>? _factory;

    public async Task InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(MsSqlPassword)
            .Build();

        await _msSqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_msSqlContainer.GetConnectionString())
            .Options;

        _factory = new TestDbContextFactory(options);
        await using var ctx = _factory.CreateDbContext();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_msSqlContainer is not null)
            await _msSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task CopyVersion_WithAllRelatedEntities_CopiesEverythingIncludingEquipment()
    {
        _factory.Should().NotBeNull();
        var seeded = await SeedSourceVersionAsync(_factory!);
        var copyService = CreateCopyService(_factory!, canEdit: true, canAccessProject: true);

        var result = await copyService.CopyVersionAsync(
            seeded.ProjectId,
            seeded.SourceVersionId,
            "Copy v2");

        result.Should().NotBeNull();
        result.Id.Should().NotBe(seeded.SourceVersionId);
        result.VersionName.Should().Be("Copy v2");

        await using var ctx = await _factory!.CreateDbContextAsync();

        var newPpvs = await ctx.ProjectPhysicalVolumes
            .Where(pv => pv.ProjectAnalysisVersionId == result.Id)
            .ToListAsync();

        newPpvs.Should().HaveCount(3);
        var newRoot = newPpvs.Single(pv => pv.ParentId == null);
        var newChildren = newPpvs.Where(pv => pv.ParentId != null).ToList();
        newChildren.Should().HaveCount(2);
        newChildren.Should().OnlyContain(c => c.ParentId == newRoot.Id);
        newChildren.Should().OnlyContain(c => c.ParentId != seeded.SourceRootPpvId);

        var newLis = await ctx.ProjectLaborIntensities
            .Where(li => li.ProjectAnalysisVersionId == result.Id)
            .ToListAsync();

        newLis.Should().HaveCount(2);
        var newPpvIds = newPpvs.Select(p => p.Id).ToHashSet();
        newLis.Should().OnlyContain(li =>
            li.ProjectPhysicalVolumeId.HasValue && newPpvIds.Contains(li.ProjectPhysicalVolumeId.Value));

        var newIterations = await ctx.DataImportIterations
            .Where(i => i.ProjectAnalysisVersionId == result.Id)
            .ToListAsync();
        newIterations.Should().HaveCount(2);

        var newEquipment = await ctx.ProjectEquipments
            .Where(e => e.ProjectAnalysisVersionId == result.Id)
            .ToListAsync();
        newEquipment.Should().HaveCount(5);

        newEquipment.Sum(e => e.Quantity).Should().Be(seeded.ExpectedEquipmentQuantitySum);
        newRoot.Quantity.Should().Be(seeded.RootQuantity);
        newLis.Sum(li => li.PlannedHours).Should().Be(seeded.ExpectedLiPlannedHoursSum);
    }

    [Fact]
    public async Task CopyVersion_WithoutEditPermission_ThrowsForbidden()
    {
        _factory.Should().NotBeNull();
        var seeded = await SeedSourceVersionAsync(_factory!);
        var copyService = CreateCopyService(_factory!, canEdit: false, canAccessProject: true);

        var act = () => copyService.CopyVersionAsync(seeded.ProjectId, seeded.SourceVersionId, "Copy denied");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CopyVersion_NonExistentVersion_ThrowsNotFound()
    {
        _factory.Should().NotBeNull();
        var seeded = await SeedSourceVersionAsync(_factory!);
        var copyService = CreateCopyService(_factory!, canEdit: true, canAccessProject: true);

        var act = () => copyService.CopyVersionAsync(seeded.ProjectId, 9_999_999, "Ghost");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static ProjectVersionDeepCopyService CreateCopyService(
        IDbContextFactory<AppDbContext> factory,
        bool canEdit,
        bool canAccessProject)
    {
        var accessLevel = canEdit ? AccessLevel.Edit : AccessLevel.Read;
        var permissionService = new PermissionService(
            (_, _) => Task.FromResult<(int RoleId, string RoleName)?>((1, nameof(UserRole.ProjectManager))),
            (_, _) => Task.FromResult<IReadOnlyDictionary<string, AccessLevel>>(
                new Dictionary<string, AccessLevel>
                {
                    [PermissionType.ProjectAnalysisVersions.ToObjectName()] = accessLevel,
                    [PermissionType.ProjectDataUpload.ToObjectName()] = accessLevel,
                }));

        var visibility = new ConfigurableProjectVisibilityService(canAccessProject);

        return new ProjectVersionDeepCopyService(
            factory.CreateDbContext(),
            new StubCurrentUserService(userId: 1),
            permissionService,
            visibility,
            NullLogger<ProjectVersionDeepCopyService>.Instance);
    }

    private static async Task<SeedResult> SeedSourceVersionAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var ctx = await factory.CreateDbContextAsync();

        ctx.Projects.Add(new Project { Code = "PRJ", Name = "Deep Copy Test" });
        await ctx.SaveChangesAsync();
        var projectId = ctx.Projects.Select(p => p.Id).Single();

        ctx.PhysicalVolumeTypes.Add(new PhysicalVolumeType { Name = "Assessment", Code = "A" });
        await ctx.SaveChangesAsync();
        var volumeTypeId = ctx.PhysicalVolumeTypes.Select(t => t.Id).Single();

        var catalogPv1 = new PhysicalVolume { Code = "PV-1", Name = "PV 1", VolumeTypeId = volumeTypeId, Coefficient = 1m };
        var catalogPv2 = new PhysicalVolume { Code = "PV-2", Name = "PV 2", VolumeTypeId = volumeTypeId, Coefficient = 1m };
        ctx.PhysicalVolumes.AddRange(catalogPv1, catalogPv2);
        await ctx.SaveChangesAsync();

        var sourceVersion = new ProjectAnalysisVersion
        {
            ProjectId = projectId,
            Name = "Source v1",
            VersionName = "Source v1",
            Direction = AnalysisDirection.Fact,
        };
        ctx.ProjectAnalysisVersions.Add(sourceVersion);
        await ctx.SaveChangesAsync();

        var root = new ProjectPhysicalVolume
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = sourceVersion.Id,
            PhysicalVolumeId = catalogPv1.Id,
            ParentId = null,
            Quantity = 100m,
            WorkId = "ROOT",
        };
        ctx.ProjectPhysicalVolumes.Add(root);
        await ctx.SaveChangesAsync();

        ctx.ProjectPhysicalVolumes.AddRange(
            new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                PhysicalVolumeId = catalogPv1.Id,
                ParentId = root.Id,
                Quantity = 10m,
                WorkId = "CH-1",
            },
            new ProjectPhysicalVolume
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                PhysicalVolumeId = catalogPv2.Id,
                ParentId = root.Id,
                Quantity = 20m,
                WorkId = "CH-2",
            });
        await ctx.SaveChangesAsync();

        var childPpvs = await ctx.ProjectPhysicalVolumes
            .Where(pv => pv.ProjectAnalysisVersionId == sourceVersion.Id && pv.ParentId == root.Id)
            .OrderBy(pv => pv.WorkId)
            .ToListAsync();

        ctx.ProjectLaborIntensities.AddRange(
            new ProjectLaborIntensity
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                DocumentationCode = "DOC-A",
                ProjectPhysicalVolumeId = childPpvs[0].Id,
                PlannedHours = 50m,
                ActualHours = 40m,
            },
            new ProjectLaborIntensity
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                DocumentationCode = "DOC-B",
                ProjectPhysicalVolumeId = childPpvs[1].Id,
                PlannedHours = 30m,
                ActualHours = 25m,
            });

        ctx.DataImportIterations.AddRange(
            new DataImportIteration
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                DataType = DataImportType.PhysicalVolume,
                ImportedAt = DateTime.UtcNow.AddDays(-2),
                RecordCount = 3,
                Description = "PV import",
            },
            new DataImportIteration
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                DataType = DataImportType.Equipment,
                ImportedAt = DateTime.UtcNow.AddDays(-1),
                RecordCount = 5,
                Description = "EQ import",
            });

        var equipments = Enumerable.Range(1, 5)
            .Select(i => new Equipment { Name = $"EQ-{i}", Coefficient = 1m, CatalogKeyHash = $"hash-{i}" })
            .ToList();
        ctx.Equipments.AddRange(equipments);
        await ctx.SaveChangesAsync();

        for (var i = 0; i < equipments.Count; i++)
        {
            ctx.ProjectEquipments.Add(new ProjectEquipment
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = sourceVersion.Id,
                EquipmentId = equipments[i].Id,
                Quantity = i + 1,
                CatalogName = equipments[i].Name,
            });
        }

        await ctx.SaveChangesAsync();

        return new SeedResult(
            projectId,
            sourceVersion.Id,
            root.Id,
            RootQuantity: 100m,
            ExpectedEquipmentQuantitySum: 15m,
            ExpectedLiPlannedHoursSum: 80m);
    }

    private sealed record SeedResult(
        int ProjectId,
        int SourceVersionId,
        int SourceRootPpvId,
        decimal RootQuantity,
        decimal ExpectedEquipmentQuantitySum,
        decimal ExpectedLiPlannedHoursSum);

    private sealed class ConfigurableProjectVisibilityService(bool canAccess) : IProjectVisibilityService
    {
        public Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> CanAccessProjectAsync(int userId, int projectId, AccessLevel requiredLevel, CancellationToken cancellationToken = default) =>
            Task.FromResult(canAccess && requiredLevel <= AccessLevel.Edit);

        public Task<IReadOnlyList<int>> GetAccessibleProjectIdsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>([]);

        public Task<IReadOnlyList<ProjectPermissionDto>> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProjectPermissionDto>>([]);

        public Task SaveProjectPermissionsAsync(int projectId, IReadOnlyList<ProjectPermissionUpsertRequest> participants, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> RemoveProjectPermissionAsync(int projectId, int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
