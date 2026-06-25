namespace EngineeringPerformance.Application.Export;

/// <summary>
/// Экспорт списка проектов (каталог) — без прямого доступа к DbContext из API.
/// </summary>
public interface IProjectsCatalogExportService
{
    Task<byte[]> ExportAllToExcelAsync(CancellationToken cancellationToken = default);

    Task<byte[]> ExportAllToPdfAsync(CancellationToken cancellationToken = default);

    Task<int?> GetFirstVersionIdAsync(int projectId, CancellationToken cancellationToken = default);
}
