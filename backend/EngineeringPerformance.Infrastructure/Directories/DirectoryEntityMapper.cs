using System.Text.Json;
using System.Text.Json.Nodes;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Infrastructure.Directories;

internal static class DirectoryEntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly HashSet<string> BasePropertyNames =
    [
        "id", "name", "parentId", "sortOrder", "isArchived", "createdAt", "modifiedAt",
    ];

    /// <summary>Навигационные коллекции EF — не отдаём в properties справочника.</summary>
    private static readonly HashSet<string> NavigationPropertyNames =
    [
        "children", "parent", "projectPhysicalVolumes", "laborIntensities",
        "classifier", "unit", "iicWorkDirection", "options", "physicalVolumeKfo",
    ];

    private static readonly HashSet<string> LaborIntensityHiddenPropertyNames =
    [
        "normPpr", "normOpr", "normPd", "normRd",
    ];

    public static DirectoryEntryDto ToDto<T>(T entity) where T : DirectoryItem
    {
        // Важно: сериализуем runtime-тип (PhysicalVolumeBer и т.д.), а не DirectoryItem —
        // иначе GetById (возвращает DirectoryItem?) отдаёт пустой Properties.
        var node = JsonSerializer.SerializeToNode(entity, entity.GetType(), JsonOptions)!.AsObject();
        var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var hideLaborNorms = entity.GetType().Name == "LaborIntensity";

        foreach (var property in node)
        {
            if (BasePropertyNames.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                continue;

            if (NavigationPropertyNames.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                continue;

            if (hideLaborNorms && LaborIntensityHiddenPropertyNames.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                continue;

            if (property.Value is JsonArray or JsonObject)
                continue;

            if (property.Value is not null)
                properties[property.Key] = JsonSerializer.Deserialize<JsonElement>(property.Value.ToJsonString())!;
        }

        return new DirectoryEntryDto(
            entity.Id,
            entity.Name,
            entity.ParentId,
            entity.SortOrder,
            entity.IsArchived,
            entity.CreatedAt,
            entity.ModifiedAt,
            properties);
    }

    public static T Merge<T>(DirectoryWriteRequest request, T? existing = null) where T : DirectoryItem, new()
    {
        var entity = existing ?? new T();
        entity.Name = request.Name.Trim();
        entity.ParentId = request.ParentId;
        entity.SortOrder = request.SortOrder;

        if (request.Properties is null || request.Properties.Count == 0)
            return entity;

        var node = JsonSerializer.SerializeToNode(entity, typeof(T), JsonOptions)!.AsObject();
        foreach (var (key, value) in request.Properties)
            node[key] = JsonSerializer.SerializeToNode(value, JsonOptions);

        return JsonSerializer.Deserialize<T>(node, JsonOptions)!;
    }

    public static IReadOnlyList<DirectoryTreeNodeDto> BuildTree<T>(IReadOnlyList<T> items) where T : DirectoryItem
    {
        var lookup = items.ToLookup(x => x.ParentId);
        DirectoryTreeNodeDto BuildNode(T item) =>
            new(
                item.Id,
                item.Name,
                item.ParentId,
                item.SortOrder,
                item.IsArchived,
                ToDto(item).Properties,
                lookup[item.Id].OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(BuildNode).ToList());

        return lookup[null]
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(BuildNode)
            .ToList();
    }
}
