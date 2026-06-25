using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.File;

namespace EngineeringPerformance.Infrastructure.Logging;

/// <summary>
/// Централизованная настройка Serilog: консоль, JSON-файл и in-memory для Log Viewer.
/// </summary>
public static class SerilogLogger
{
    public const string DefaultJsonLogDirectory = "logs";

    public static LoggerConfiguration ConfigureAdminLogging(
        LoggerConfiguration configuration,
        SerilogLogStore store,
        string? jsonLogDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(jsonLogDirectory)
            ? DefaultJsonLogDirectory
            : jsonLogDirectory;

        store.JsonLogDirectory = directory;
        Directory.CreateDirectory(directory);

        return configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "EngineeringPerformance.Api")
            .WriteTo.Console()
            .WriteTo.Sink(new SerilogInMemorySink(store), restrictedToMinimumLevel: LogEventLevel.Debug)
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(directory, "app-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true);
    }
}
