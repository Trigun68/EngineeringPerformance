namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Мост часов факт vs план по корневой группе IIC:
/// эффект объёма + эффект производительности = H_fact − H_plan.
/// </summary>
public sealed record HoursBridgeLineDto(
    int GroupId,
    string GroupName,
    decimal VolumeEffect,
    decimal ProductivityEffect)
{
    public decimal TotalDelta => VolumeEffect + ProductivityEffect;
}

public sealed record HoursBridgeResultDto(
    IReadOnlyList<HoursBridgeLineDto> Lines,
    decimal TotalVolumeEffect,
    decimal TotalProductivityEffect)
{
    public static HoursBridgeResultDto Empty { get; } = new([], 0m, 0m);

    public decimal TotalDelta => TotalVolumeEffect + TotalProductivityEffect;
}

/// <summary>Точка накопительных часов (S-curve) после сортировки по дате.</summary>
public sealed record CumulativeHoursPointDto(DateTime Date, decimal CumulativeHours, string? Label = null);

/// <summary>Часы версии на дату — вход S-curve.</summary>
public sealed record HoursAtDateSample(DateTime Date, decimal Hours, string? Label = null);

/// <summary>Столбец «Z от 0»: наименование (проект или группа) и Z.</summary>
public sealed record ZeroBaselineBarDto(string Name, decimal ZScore, decimal Weight = 0m);

/// <summary>Сэмпл для водопада/столбцов Z от 0 (топ-N + «Прочие»).</summary>
public sealed record ZeroBaselineSample(string Name, decimal? ZScore, decimal Weight = 0m);

/// <summary>Сетка «по группе IIC — столбцы проектов».</summary>
public sealed record GroupZeroBaselineDto(
    int GroupId,
    string GroupName,
    IReadOnlyList<ZeroBaselineBarDto> Bars);

/// <summary>
/// Покрытие часов: доля с известным Z и доля с парой план/факт (правила моста).
/// Доли считает движок, UI только отображает.
/// </summary>
public sealed record HoursCoverageDto(
    decimal HoursTotal,
    decimal HoursWithZ,
    decimal HoursWithBridgePair,
    decimal HoursEligibleForBridge,
    decimal? ShareWithZ,
    decimal? ShareWithBridgePair)
{
    public static HoursCoverageDto Empty { get; } = new(0m, 0m, 0m, 0m, null, null);
}

/// <summary>Полоса взвешенных перцентилей вокруг среднего Z.</summary>
public sealed record WeightedPercentileBandDto(decimal P10, decimal P90);

/// <summary>Точка полосы p10–p90 на дату.</summary>
public sealed record PercentileBandPointDto(DateTime Date, decimal P10, decimal P90);

/// <summary>Вход «кто тянет» портфель: вклад проекта в средневзвешенный Z.</summary>
public sealed record MoverSample(
    int ProjectId,
    string ProjectCode,
    string ProjectName,
    decimal? ZScore,
    decimal Weight);

/// <summary>Строка таблицы «кто тянет»: Contribution = Z×вес / Σвесов.</summary>
public sealed record MoverRowDto(
    int ProjectId,
    string ProjectCode,
    string ProjectName,
    decimal? ZScore,
    decimal Weight,
    decimal Contribution);

/// <summary>Часы одного проекта по датам — вход тонких S-curve.</summary>
public sealed record ProjectHoursSeriesSample(
    int ProjectId,
    string Name,
    IReadOnlyList<HoursAtDateSample> Points);

/// <summary>Именованная накопительная кривая (проект или «Прочие»).</summary>
public sealed record NamedSCurveDto(string Name, IReadOnlyList<CumulativeHoursPointDto> Points);
