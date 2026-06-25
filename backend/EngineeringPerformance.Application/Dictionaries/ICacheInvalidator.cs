namespace EngineeringPerformance.Application.Dictionaries;

/// <summary>
/// Интерфейс для инвалидации кэша справочников.
/// Вызывается при изменении данных справочников в БД.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Удаляет кэш справочников. Вызывать после Create/Update/Delete операций.
    /// </summary>
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
