using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>EF-translatable фильтры для ProjectPhysicalVolume (extension-методы в LINQ-to-SQL не переводятся).</summary>
internal static class ProjectPhysicalVolumeQueryExtensions
{
    public static IQueryable<ProjectPhysicalVolume> WhereAssessmentWithClassifier(
        this IQueryable<ProjectPhysicalVolume> query) =>
        query.Where(pv =>
            ((pv.PhysicalVolumeKfoId != null && pv.PhysicalVolumeKfo!.IsAssessment == true)
             || (pv.PhysicalVolumeBerId != null && pv.PhysicalVolumeBer!.IsAssessment == true))
            && ((pv.PhysicalVolumeKfoId != null && pv.PhysicalVolumeKfo!.ClassifierId != null)
                || (pv.PhysicalVolumeBerId != null && pv.PhysicalVolumeBer!.ClassifierId != null)));

    public static IQueryable<ProjectPhysicalVolume> WherePositiveAssessmentWithClassifier(
        this IQueryable<ProjectPhysicalVolume> query) =>
        query.Where(pv => pv.Quantity > 0).WhereAssessmentWithClassifier();
}
