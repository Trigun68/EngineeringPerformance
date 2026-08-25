namespace EngineeringPerformance.Application.Import;

/// <summary>Пакет JSON для физ. объёмов версии. Excel-канал не затрагивает.</summary>
public sealed record PhysicalVolumeIngestRequest(
    IReadOnlyList<PhysicalVolumeIngestItem> Items,
    string Mode = "replace",
    string? Description = null,
    bool AllowEmptyReplace = false);

/// <summary>Строка ФО: номенклатура по <c>epCode</c> (как Excel) или по внутреннему Id.</summary>
public sealed record PhysicalVolumeIngestItem(
    decimal Quantity,
    string? EpCode = null,
    int? PhysicalVolumeId = null,
    string? FacilityCode = null,
    string? FacilityName = null,
    string? WorkId = null,
    string? WorkName = null,
    string? BindingType = null,
    string? DocumentSet = null,
    string? CatalogId = null,
    string? Tag = null,
    string? WorkComment = null,
    string? EstimationBasis = null);

/// <summary>Пакет JSON оборудования версии.</summary>
public sealed record EquipmentIngestRequest(
    IReadOnlyList<EquipmentIngestItem> Items,
    string Mode = "replace",
    string? Description = null,
    bool AllowEmptyReplace = false);

/// <summary>Строка оборудования: каталог проекта по имени+единице или <c>equipmentId</c>.</summary>
public sealed record EquipmentIngestItem(
    decimal Quantity,
    string? CatalogName = null,
    string? UnitName = null,
    int? UnitId = null,
    int? EquipmentId = null,
    decimal? QuantityInWork = null,
    string? PositionNumber = null,
    string? FacilityCode = null,
    string? FacilityName = null,
    string? DocumentSet = null,
    string? DiagramNumber = null,
    string? ShortName = null,
    string? TypeMark = null,
    string? CatalogId = null,
    string? Tag = null,
    string? FullEquipmentId = null,
    string? SupplyStandard = null,
    string? OlReference = null,
    string? EstimationBasis = null);

/// <summary>Пакет JSON трудоёмкости версии.</summary>
public sealed record LaborIntensityIngestRequest(
    IReadOnlyList<LaborIntensityIngestItem> Items,
    string Mode = "replace",
    string? Description = null,
    bool AllowEmptyReplace = false,
    bool AddMissingLaborToCatalog = false);

/// <summary>Строка трудоёмкости: тип документа как в Excel (не колонка «направление»).</summary>
public sealed record LaborIntensityIngestItem(
    string DocumentTypeName,
    decimal PlannedHours,
    decimal ActualHours = 0,
    string? Brand = null,
    string? Notes = null);

/// <summary>Предпросмотр трудоёмкости без файла.</summary>
public sealed record LaborIntensityIngestPreviewRequest(
    IReadOnlyList<string> DocumentTypeNames);
