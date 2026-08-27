using System.Text.Json;

namespace EngineeringPerformance.Application.Directories;

public sealed record DirectoryWriteRequest(
    string Name,
    int? ParentId = null,
    int SortOrder = 0,
    Dictionary<string, JsonElement>? Properties = null);

public sealed record SortOrderRequest(int? ParentId, int SortOrder);

public sealed record DirectoryEntryDto(
    int Id,
    string Name,
    int? ParentId,
    int SortOrder,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    IReadOnlyDictionary<string, JsonElement> Properties,
    int RecalculatedVersionCount = 0);

public sealed record DirectoryTreeNodeDto(
    int Id,
    string Name,
    int? ParentId,
    int SortOrder,
    bool IsArchived,
    IReadOnlyDictionary<string, JsonElement> Properties,
    IReadOnlyList<DirectoryTreeNodeDto> Children);

public sealed record CharacteristicOptionDto(
    int Id,
    int CharacteristicId,
    string Name,
    decimal? ValuePercent,
    int SortOrder);

public sealed record CharacteristicOptionWriteRequest(
    string Name,
    decimal? ValuePercent = null,
    int SortOrder = 0);

/// <summary>
/// Пагинированный ответ для справочников.
/// </summary>
public sealed record PagedDirectoryResponse(
    IReadOnlyList<DirectoryEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
