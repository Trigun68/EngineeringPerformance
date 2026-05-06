namespace EngineeringPerformance.Core.DTOs;

public sealed record DisciplineAnalysisDto(
    int Id,
    int? ParentId,
    string DisciplineName,
    decimal LaborHours,
    decimal PercentOfTotal,
    decimal? ZScore,
    string Assessment);
