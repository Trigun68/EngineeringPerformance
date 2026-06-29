using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Engineering Performance — Database Initializer ===");
Console.WriteLine();

var connectionString = GetConnectionString();
Console.WriteLine($"Connection string: {ConnectionStringMasking.MaskForDisplay(connectionString)}");
Console.WriteLine();

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer(connectionString);

// Команда «--import-labor <project-code> <version-name> <file>» — одноразовая загрузка файла TF.
if (args.Length >= 4 && args[0] == "--import-labor")
{
    return await RunImportLaborAsync(optionsBuilder.Options, args[1], args[2], args[3]);
}

await using var context = new AppDbContext(optionsBuilder.Options);

Console.WriteLine("Testing database connection...");
try
{
    var canConnect = await context.Database.CanConnectAsync();
    Console.WriteLine(canConnect ? "Connection successful." : "Connection failed.");
    if (!canConnect)
    {
        Console.WriteLine("Check your SQL Server instance and connection string.");
        return 1;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Connection error: {ex.Message}");
    return 1;
}

Console.WriteLine();
Console.WriteLine("Applying migrations...");

try
{
    await DatabaseSeeder.MigrateAsync(context);
    Console.WriteLine("Migrations applied.");

    Console.WriteLine("Seeding database...");
    await DatabaseSeeder.SeedAsync(context);
    Console.WriteLine();
    Console.WriteLine("Database initialized successfully!");
    Console.WriteLine();
    await PrintSummaryAsync(context);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing database: {ex.Message}");
    return 1;
}

static async Task<int> RunImportLaborAsync(DbContextOptions<AppDbContext> options, string projectCode, string versionName, string filePath)
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"ERROR: file not found: {filePath}");
        return 2;
    }

    var factory = new SingleOptionsFactory(options);
    await using var ctx = await factory.CreateDbContextAsync();

    var project = await ctx.Projects.FirstOrDefaultAsync(p => p.Code == projectCode);
    if (project is null)
    {
        Console.WriteLine($"ERROR: project with Code '{projectCode}' not found.");
        return 3;
    }

    var version = await ctx.ProjectAnalysisVersions
        .FirstOrDefaultAsync(v => v.ProjectId == project.Id && v.VersionName == versionName);
    if (version is null)
    {
        version = new ProjectAnalysisVersion { ProjectId = project.Id, VersionName = versionName };
        ctx.ProjectAnalysisVersions.Add(version);
        await ctx.SaveChangesAsync();
        Console.WriteLine($"Created version '{versionName}' (Id={version.Id}) for project '{projectCode}'.");
    }

    var service = new DataImportService(factory, NullLogger<DataImportService>.Instance);
    Console.WriteLine($"Importing labor intensity: {filePath}");
    var result = await service.ImportFromFileAsync(
        project.Id,
        version.Id,
        DataImportType.LaborIntensity,
        filePath,
        description: $"CLI import from {Path.GetFileName(filePath)}");

    Console.WriteLine();
    Console.WriteLine($"Success={result.Success}, RecordsImported={result.RecordsImported}");
    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
    {
        Console.WriteLine($"Error: {result.ErrorMessage}");
    }
    if (!string.IsNullOrWhiteSpace(result.SuccessDetails))
    {
        Console.WriteLine($"Details: {result.SuccessDetails}");
    }

    return result.Success ? 0 : 4;
}

static string GetConnectionString()
{
    var envVar = Environment.GetEnvironmentVariable("EP_CONNECTION_STRING");
    if (!string.IsNullOrWhiteSpace(envVar))
        return envVar;

    return new AppSettings().ConnectionString;
}

static async Task PrintSummaryAsync(AppDbContext context)
{
    var userCount = await context.Users.CountAsync();
    var projectCount = await context.Projects.CountAsync();
    var versionCount = await context.ProjectAnalysisVersions.CountAsync();
    var volumeTypeCount = await context.PhysicalVolumeTypes.CountAsync();
    var directionCount = await context.IicWorkDirections.CountAsync();
    var normCount = await context.IicWorkDirectionNorms.CountAsync();
    var accessRightCount = await context.AccessRights.CountAsync();

    Console.WriteLine("--- Summary ---");
    Console.WriteLine($"Users:              {userCount}");
    Console.WriteLine($"Projects:           {projectCount}");
    Console.WriteLine($"Analysis Versions:  {versionCount}");
    Console.WriteLine($"Volume Types:       {volumeTypeCount}");
    Console.WriteLine($"Work Directions:    {directionCount}");
    Console.WriteLine($"Direction Norms:    {normCount}");
    Console.WriteLine($"Access Rights:      {accessRightCount}");
    Console.WriteLine();
    Console.WriteLine("--- Test Accounts ---");
    Console.WriteLine("  admin / admin       (Administrator)");
    Console.WriteLine("  manager / manager   (Project Manager)");
    Console.WriteLine("  observer / observer (Observer)");
}

internal sealed class SingleOptionsFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AppDbContext(options));
}
