namespace EngineeringPerformance.Core.Services;

/// <summary>
/// Копирование версии анализа проекта вместе со связанными строками данных.
/// Реализация выполняет операцию в одной транзакции.
/// </summary>
public interface IProjectAnalysisVersionCopyService
{
    /// <summary>
    /// Создаёт новую версию анализа на базе указанной source-версии.
    /// Копируются: сам ProjectAnalysisVersion (с теми же Direction/PhysicalVolumeTypeId/ProjectId),
    /// все ProjectPhysicalVolume (всегда), все ProjectLaborIntensity (только для Direction=Fact),
    /// все DataImportIteration source-версии 1-в-1 (с теми же ImportedAt/SourceFileHash/SourceFileName/RecordCount/DataType/ImportedByUserId).
    /// AnalysisVersionSnapshot НЕ копируется — пересоздаётся при первом расчёте по актуальным нормам IIC.
    /// </summary>
    /// <param name="sourceVersionId">Id копируемой версии.</param>
    /// <param name="newVersionName">Имя новой версии (обязательно).</param>
    /// <param name="newDescription">Описание новой версии (опционально).</param>
    /// <returns>Id созданной версии.</returns>
    Task<int> CopyVersionAsync(int sourceVersionId, string newVersionName, string? newDescription, CancellationToken cancellationToken = default);
}
