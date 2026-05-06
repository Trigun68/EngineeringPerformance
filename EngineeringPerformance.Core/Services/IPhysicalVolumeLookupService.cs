using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

/// <summary>
/// Лёгкая проекция номенклатуры физических объёмов для выпадающих списков
/// (выбор родителя и т.п.). Возвращает только поля, нужные для отображения
/// и связывания, без материализации полных сущностей и без JOIN-ов.
/// </summary>
public interface IPhysicalVolumeLookupService
{
    Task<IReadOnlyList<PhysicalVolumeLookupItem>> GetLookupAsync(
        bool includeArchived = false,
        int? excludeId = null,
        CancellationToken cancellationToken = default);
}
