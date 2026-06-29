using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>Связь хранимого количества по проекту с единицами группы физ. объёмов ИИС (номенклатура × коэффициент).</summary>
internal static class ProjectPhysicalVolumeIicUnits
{
    public static decimal QuantityInIicGroupUnits(this ProjectPhysicalVolume pv) =>
        pv.Quantity * pv.GetCoefficient();

    /// <summary>Из объёма в единицах ИИС (как в отчётах) — в <see cref="ProjectPhysicalVolume.Quantity"/> для справочника с данным коэффициентом.</summary>
    public static decimal ProjectQuantityFromIicGroupUnits(decimal volumeInIicUnits, decimal catalogCoefficient)
    {
        var k = catalogCoefficient <= 0m ? 1m : catalogCoefficient;
        return volumeInIicUnits / k;
    }
}
