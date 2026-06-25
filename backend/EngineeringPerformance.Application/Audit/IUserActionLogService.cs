using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Audit;

public interface IUserActionLogService
{
    Task LogAsync(
        int userId,
        UserActionType actionType,
        string? entityType = null,
        int? entityId = null,
        string? description = null,
        string? ipOrHost = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserActionLogDto>> GetAsync(
        UserActionLogFilter filter,
        int skip = 0,
        int take = 1000,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<int> ArchiveAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    Task<int> RestoreAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    Task<byte[]> ExportXlsxAsync(
        UserActionLogFilter filter,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
}
