using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

public sealed class ProjectCharacteristicSelectionServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory(out AppDbContext seedContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        seedContext = factory.CreateDbContext();
        return factory;
    }

    private static (Project project, ProjectCharacteristic characteristic, ProjectCharacteristicOption optionA, ProjectCharacteristicOption optionB)
        SeedFixture(AppDbContext context)
    {
        var project = new Project { Id = 1, Code = "PRJ-001", Name = "Test" };
        var characteristic = new ProjectCharacteristic { Id = 10, Name = "Сложность" };
        var optionA = new ProjectCharacteristicOption { Id = 100, CharacteristicId = 10, Name = "Низкая", ValuePercent = 5m };
        var optionB = new ProjectCharacteristicOption { Id = 101, CharacteristicId = 10, Name = "Высокая", ValuePercent = 15m };
        context.Projects.Add(project);
        context.ProjectCharacteristics.Add(characteristic);
        context.ProjectCharacteristicOptions.AddRange(optionA, optionB);
        context.SaveChanges();
        return (project, characteristic, optionA, optionB);
    }

    [Fact]
    public async Task Create_PersistsSelection()
    {
        var factory = CreateFactory(out var seedContext);
        var (project, characteristic, optionA, _) = SeedFixture(seedContext);
        var service = new ProjectCharacteristicSelectionService(factory);

        var entity = await service.CreateAsync(project.Id, characteristic.Id, optionA.Id);

        entity.Id.Should().BeGreaterThan(0);

        await using var verifyContext = factory.CreateDbContext();
        verifyContext.ProjectCharacteristicSelections.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByProject_ReturnsRowsWithJoinedNamesAndValuePercent()
    {
        var factory = CreateFactory(out var seedContext);
        var (project, characteristic, _, optionB) = SeedFixture(seedContext);
        var service = new ProjectCharacteristicSelectionService(factory);
        await service.CreateAsync(project.Id, characteristic.Id, optionB.Id);

        var rows = await service.GetByProjectAsync(project.Id);

        rows.Should().HaveCount(1);
        var row = rows[0];
        row.CharacteristicId.Should().Be(characteristic.Id);
        row.CharacteristicName.Should().Be("Сложность");
        row.OptionId.Should().Be(optionB.Id);
        row.OptionName.Should().Be("Высокая");
        row.ValuePercent.Should().Be(15m);
    }

    [Fact]
    public async Task Update_ChangesOption()
    {
        var factory = CreateFactory(out var seedContext);
        var (project, characteristic, optionA, optionB) = SeedFixture(seedContext);
        var service = new ProjectCharacteristicSelectionService(factory);
        var created = await service.CreateAsync(project.Id, characteristic.Id, optionA.Id);

        await service.UpdateAsync(created.Id, characteristic.Id, optionB.Id);

        var rows = await service.GetByProjectAsync(project.Id);
        rows[0].OptionId.Should().Be(optionB.Id);
        rows[0].ValuePercent.Should().Be(15m);
    }

    [Fact]
    public async Task Update_NonExistent_Throws()
    {
        var factory = CreateFactory(out _);
        var service = new ProjectCharacteristicSelectionService(factory);

        var act = () => service.UpdateAsync(999, 1, 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_RemovesRow()
    {
        var factory = CreateFactory(out var seedContext);
        var (project, characteristic, optionA, _) = SeedFixture(seedContext);
        var service = new ProjectCharacteristicSelectionService(factory);
        var created = await service.CreateAsync(project.Id, characteristic.Id, optionA.Id);

        await service.DeleteAsync(created.Id);

        (await service.GetByProjectAsync(project.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_NonExistent_DoesNotThrow()
    {
        var factory = CreateFactory(out _);
        var service = new ProjectCharacteristicSelectionService(factory);

        var act = () => service.DeleteAsync(999);

        await act.Should().NotThrowAsync();
    }
}
