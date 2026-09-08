using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Excel-импорт физических объёмов (replace, привязка к каталогу КФО/БЕР).</summary>
public interface IPhysicalVolumeExcelImportService
{
    Task<DataImportResultDto> ImportPhysicalVolumesAsync(
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source);
}

internal sealed partial class PhysicalVolumeExcelImportService : IPhysicalVolumeExcelImportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<PhysicalVolumeExcelImportService> _logger;

    public PhysicalVolumeExcelImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<PhysicalVolumeExcelImportService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }
}
