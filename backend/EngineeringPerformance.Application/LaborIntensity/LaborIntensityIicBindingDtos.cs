namespace EngineeringPerformance.Application.LaborIntensity;

/// <summary>Период привязки позиции справочника трудоёмкости к группе ИИС.</summary>
public sealed record LaborIntensityIicBindingDto(
    int Id,
    int LaborIntensityId,
    int? IicWorkDirectionId,
    string? IicGroupName,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);

public sealed record LaborIntensityIicBindingWriteRequest(
    int? IicWorkDirectionId,
    DateTime EffectiveFrom);
