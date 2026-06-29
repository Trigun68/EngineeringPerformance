using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Core.Services;

/// <summary>
/// Универсальный кеш справочника на уровне процесса. Позволяет получать
/// активные (не архивные) записи без повторных запросов к БД, если данные
/// в справочнике стабильны в рамках сессии приложения.
///
/// Кеш безопасно использовать из любого потока: одновременные вызовы
/// <see cref="GetAllAsync"/> не приводят к дублирующим запросам.
/// </summary>
public interface IDirectoryCache<T> where T : DirectoryItem
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Полностью сбрасывает кеш. Использовать только при массовых
    /// или непредсказуемых изменениях. Точечные правки см. <see cref="Upsert"/>.
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Точечно заменяет/добавляет запись в кеше без обращения к БД.
    /// Не делает ничего, если кеш ещё не прогрет. Реализация — copy-on-write,
    /// читатели кеша получают консистентный снэпшот.
    /// </summary>
    void Upsert(T item);

    /// <summary>
    /// Точечно удаляет запись из кеша. Аналогично <see cref="Upsert"/>,
    /// без обращения к БД и без полного сброса.
    /// </summary>
    void Remove(int id);
}
