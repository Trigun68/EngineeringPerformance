using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

/// <summary>
/// Лёгкая проекция групп физ. объёмов (IIC) для выпадающих списков
/// (выбор родителя и т.п.). Возвращает только поля, нужные для отображения
/// и связывания, без JOIN-ов с единицами измерения и без полной материализации.
/// </summary>
public interface IIicWorkDirectionLookupService
{
    Task<IReadOnlyList<IicWorkDirectionLookupItem>> GetLookupAsync(
        bool includeArchived = false,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
}
