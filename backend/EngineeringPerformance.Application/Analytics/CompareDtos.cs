namespace EngineeringPerformance.Application.Analytics;

public sealed record VersionCompareRowDto(
    int WorkDirectionId,
    int? ParentId,
    string Name,
    decimal? ZScoreA,
    decimal? ZScoreB,
    decimal? ZDelta,
    string InterpretationA,
    string InterpretationB);

public sealed record VersionCompareFacilityRowDto(
    string FacilityCode,
    string FacilityName,
    decimal? FactLaborHoursA,
    decimal? FactLaborHoursB,
    decimal? HoursDelta,
    decimal? ZScoreA,
    decimal? ZScoreB,
    decimal? ZDelta,
    string InterpretationA,
    string InterpretationB);

public sealed record VersionCompareDetailRowDto(
    string FacilityCode,
    string FacilityName,
    string DocumentSet,
    string PvCode,
    string PvName,
    string WorkId,
    string WorkName,
    decimal? QuantityA,
    decimal? QuantityB,
    decimal? QuantityDelta,
    decimal? FactLaborHoursA,
    decimal? FactLaborHoursB,
    decimal? HoursDelta,
    decimal? FactProductivityA,
    decimal? FactProductivityB,
    decimal? ProductivityDelta,
    string IicGroupName,
    string IicUnitName,
    string Presence);

public sealed record VersionCompareResultDto(
    IReadOnlyList<VersionCompareRowDto> Summary,
    IReadOnlyList<VersionCompareFacilityRowDto> Facility,
    IReadOnlyList<VersionCompareDetailRowDto> Detail);

public sealed record GroupAnalyticsPlaceholderDto(string Message);
