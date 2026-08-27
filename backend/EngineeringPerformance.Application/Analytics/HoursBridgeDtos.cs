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
