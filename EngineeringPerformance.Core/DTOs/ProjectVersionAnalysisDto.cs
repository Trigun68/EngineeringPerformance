using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

/// <summary>
/// Отчёт «Анализ версии проекта». Иерархия направлений IIC (полностью раскрытая)
/// плюс отдельный блок видов работ из трудоёмкости проекта.
/// </summary>
public sealed record ProjectVersionAnalysisDto(
    string ProjectName,
    string? ProjectCode,
    string VersionName,
    IReadOnlyList<ProjectVersionDirectionDto> Directions,
    IReadOnlyList<ProjectVersionLaborIntensityDto> LaborIntensities,
    ProjectVersionAnalysisSummaryDto Summary);

/// <summary>Итоговые показатели отчёта «Анализ версии проекта».</summary>
public sealed record ProjectVersionAnalysisSummaryDto(
    decimal? TotalFactLaborHours,
    decimal? WeightedZScore,
    string WeightedZScorePercentile,
    PerformanceInterpretation? WeightedZScoreInterpretation,
    IReadOnlyList<PhysicalVolumeTotalByUnitDto> PhysicalVolumeTotalsByUnit,
    decimal? UnallocatedLaborHours);

/// <summary>Сумма физ. объёма по одной единице измерения (без смешивания разных ед.).</summary>
public sealed record PhysicalVolumeTotalByUnitDto(
    string UnitName,
    decimal TotalVolume);

/// <summary>
/// Узел иерархии IIC c показателями производительности и списком ненулевых физ. объёмов проекта.
/// </summary>
public sealed record ProjectVersionDirectionDto(
    int Id,
    int? ParentId,
    int Level,
    string Code,
    string Name,
    string? UnitName,
    decimal? FactPhysicalVolume,
    decimal? FactLaborHours,
    decimal? FactProductivity,
    decimal? FactEstimate,
    decimal? NormProductivity,
    decimal? NormEstimate,
    decimal? NormStdDev,
    decimal? ZScore,
    PerformanceInterpretation? Interpretation,
    IReadOnlyList<ProjectVersionPhysicalVolumeDto> PhysicalVolumes,
    IReadOnlyList<ProjectVersionPhysicalVolumeDto> EquipmentVolumes,
    IReadOnlyList<ProjectVersionDirectionLaborDto> DirectionLaborIntensities);

/// <summary>Запись трудоёмкости проекта, привязанная к конкретному направлению IIC.</summary>
public sealed record ProjectVersionDirectionLaborDto(
    string DocumentationCode,
    decimal PlannedHours);

/// <summary>Ненулевая позиция физ. объёма проекта в составе направления IIC.</summary>
public sealed record ProjectVersionPhysicalVolumeDto(
    string Code,
    string Name,
    string? UnitName,
    decimal Quantity,
    decimal Coefficient,
    decimal Total);

/// <summary>Запись из трудоёмкости проекта для блока «Виды работ».</summary>
public sealed record ProjectVersionLaborIntensityDto(
    string DocumentationCode,
    string? Brand,
    string? WorkDirectionName,
    decimal PlannedHours,
    decimal ActualHours);
