namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сохранение планового объёма по листу ИИС в таблицу физ. объёмов по проекту для версии анализа; пересчёт прогноза выполняется повторной загрузкой отчёта.
/// </summary>
public interface IForecastPlannedVolumePersistenceService
{
    /// <summary>
    /// Устанавливает суммарный количественный показатель по всем строкам версии, у которых номенклатура физ. объёма ссылается на классификатор <paramref name="workDirectionId"/> (любой узел ИИС).
    /// Если строк нет — создаётся одна запись по номенклатуре из справочника (с учётом типа объёма версии).
    /// </summary>
    /// <param name="quantity">Объём в единицах группы физ. объёмов ИИС (как в отчёте прогноза); в БД сохраняется <c>Quantity = quantity / Коэффициент</c> номенклатуры.</param>
    /// <exception cref="InvalidOperationException">Версия не найдена, не «План», нет подходящей номенклатуры физ. объёма.</exception>
    Task SavePlannedVolumeAsync(
        int projectId,
        int projectAnalysisVersionId,
        int workDirectionId,
        decimal quantity,
        CancellationToken cancellationToken = default);
}
