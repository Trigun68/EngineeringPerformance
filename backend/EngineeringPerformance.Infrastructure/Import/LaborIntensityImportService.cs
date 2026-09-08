using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Excel/JSON-импорт трудоёмкости: тип документа → каталог LaborIntensity (без учёта регистра).</summary>
public interface ILaborIntensityImportService
{
    Task<DataImportResultDto> ImportLaborIntensitiesAsync(
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source,
        bool addMissingToCatalog);

    Task<LaborImportPreviewDto> PreviewLaborImportAsync(string filePath);

    Task<LaborImportPreviewDto> PreviewLaborIngestAsync(
        IReadOnlyList<string> documentTypeNames,
        CancellationToken cancellationToken = default);

    Task<DataImportResultDto> IngestLaborIntensitiesAsync(
        int projectId,
        int versionId,
        IReadOnlyList<LaborIntensityIngestItem> items,
        bool isReplace,
        bool addMissingToCatalog,
        string description,
        ImportSourceMetadata source,
        CancellationToken cancellationToken = default);
}

internal sealed partial class LaborIntensityImportService : ILaborIntensityImportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<LaborIntensityImportService> _logger;

    public LaborIntensityImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<LaborIntensityImportService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }
}
