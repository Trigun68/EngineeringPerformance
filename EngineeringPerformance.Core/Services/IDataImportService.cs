using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.Services;

public interface IDataImportService
{
    Task<DataImportResult> ImportFromFileAsync(int projectId, int versionId, DataImportType dataType, string filePath, string description, bool addMissingLaborToCatalog = false);

    Task<DataImportResult> ImportFromApiAsync(int projectId, int versionId, DataImportType dataType, string apiEndpoint, string description);

    Task<IReadOnlyList<DataImportIterationInfo>> GetImportIterationsAsync(int projectId, int versionId, DataImportType dataType);

    Task<int> DeleteIterationAsync(int projectId, int versionId, DataImportType dataType, DateTime importedAt);

    /// <summary>
    /// Читает Excel-файл трудоёмкости и возвращает список «Типов документа», отсутствующих
    /// в справочнике LaborIntensities. Ничего не пишет в БД — используется интерфейсом для
    /// запроса решения у пользователя (добавить в справочник / импортировать без добавления / отменить).
    /// </summary>
    Task<LaborImportPreview> PreviewLaborImportAsync(string filePath);
}
