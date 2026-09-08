using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Excel/JSON-импорт оборудования версии (replace, каталог проекта).</summary>
public interface IEquipmentImportService
{
    Task<DataImportResultDto> ImportEquipmentAsync(
        int projectId,
        int versionId,
        string filePath,
        string description,
        ImportSourceMetadata source);

    Task<DataImportResultDto> IngestEquipmentAsync(
        int projectId,
        int versionId,
        IReadOnlyList<EquipmentIngestItem> items,
        bool isReplace,
        string description,
        ImportSourceMetadata source,
        CancellationToken cancellationToken = default);
}

internal sealed partial class EquipmentImportService : IEquipmentImportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<EquipmentImportService> _logger;
    private readonly IDictionaryService _dictionaryService;

    public EquipmentImportService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<EquipmentImportService> logger,
        IDictionaryService dictionaryService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _dictionaryService = dictionaryService;
    }
}
