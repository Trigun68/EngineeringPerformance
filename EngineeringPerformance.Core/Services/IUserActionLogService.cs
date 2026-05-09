using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.Services;

// Сервис журнала действий пользователей.
// LogAsync — точка вызова из бизнес-кода; не должен бросать исключения наружу
// (ошибки логирования не ломают основную операцию — пишутся в Serilog).
public interface IUserActionLogService
{
    Task LogAsync(
        int userId,
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserActionLogDto>> GetAsync(
        UserActionLogFilter filter,
        int skip = 0,
        int take = 1000,
        CancellationToken cancellationToken = default);

    // Экспорт всех записей в CSV (UTF-8 BOM, разделитель `;`) и удаление их из БД.
    // Операция атомарна: при ошибке записи файла удаление не происходит.
    Task<UserActionLogExportResult> ExportToFileAndPurgeAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    // Импорт записей из CSV-файла с дедупликацией по
    // (UserId, Timestamp, ActionType, EntityType, EntityId).
    Task<UserActionLogImportResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
