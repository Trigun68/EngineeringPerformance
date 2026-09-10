using ClosedXML.Excel;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Projects;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Черновик строки ФО: разбор Excel и JSON, усечение полей, запись в ProjectPhysicalVolume.
/// </summary>
internal sealed class PhysicalVolumeImportDraft
{
    public const int FacilityCodeMax = 100;
    public const int FacilityNameMax = 500;
    public const int BindingTypeMax = 200;
    public const int DocumentSetMax = 100;
    public const int CatalogIdMax = 100;
    public const int TagMax = 100;
    public const int WorkIdMax = 200;
    public const int WorkNameMax = 1000;
    public const int WorkCommentMax = 500;
    public const int EstimationBasisMax = 500;
    public const int CatalogCodeMax = 100;
    public const int CatalogNameMax = 1000;

    public decimal Quantity { get; set; }

    public string? FacilityCode { get; set; }

    public string? FacilityName { get; set; }

    public string? BindingType { get; set; }

    public string? DocumentSet { get; set; }

    public string? CatalogId { get; set; }

    public string? Tag { get; set; }

    public string? WorkId { get; set; }

    public string? WorkName { get; set; }

    public string? WorkComment { get; set; }

    public string? EstimationBasis { get; set; }

    public string? CatalogCode { get; set; }

    public string? CatalogName { get; set; }

    public static PhysicalVolumeImportDraft FromRow(
        IXLRangeRow row,
        PhysicalVolumeColumnMap map,
        Func<string?, decimal> parseDecimal,
        Func<IXLRangeRow, int, string?> getCell,
        PvCatalogRow catalogEntry)
    {
        var fbsSource = getCell(row, map.FacilitySource)?.Trim();
        var rawWorkName = getCell(row, map.WorkName)?.Trim();

        string? facilityCode;
        string? facilityName;

        if (map.SplitFacilityFromFbs)
        {
            var split = SplitFbsFacilityCodeAndName(fbsSource);
            facilityCode = split.Code;
            facilityName = split.Name;
        }
        else
        {
            facilityCode = fbsSource;
            facilityName = null;
        }

        var workId = map.WorkId > 0 ? getCell(row, map.WorkId)?.Trim() : null;

        return new PhysicalVolumeImportDraft
        {
            Quantity = parseDecimal(getCell(row, map.Quantity)),
            FacilityCode = facilityCode,
            FacilityName = facilityName,
            BindingType = getCell(row, map.BindingType)?.Trim(),
            DocumentSet = getCell(row, map.DocumentSet)?.Trim(),
            CatalogId = getCell(row, map.CatalogId)?.Trim(),
            Tag = getCell(row, map.Tag)?.Trim(),
            WorkId = workId,
            WorkName = rawWorkName,
            WorkComment = getCell(row, map.WorkComment)?.Trim(),
            EstimationBasis = getCell(row, map.EstimationBasis)?.Trim(),
            CatalogCode = catalogEntry.Code,
            CatalogName = catalogEntry.Name,
        };
    }

    private static (string? Code, string? Name) SplitFbsFacilityCodeAndName(string? fbs)
    {
        if (string.IsNullOrWhiteSpace(fbs))
        {
            return (null, null);
        }

        var idx = fbs.IndexOf(" - ", StringComparison.Ordinal);
        if (idx <= 0)
        {
            var t = fbs.Trim();
            return (Truncate(t, FacilityCodeMax), Truncate(t, FacilityNameMax));
        }

        var code = fbs[..idx].Trim();
        var name = fbs[(idx + 3)..].Trim();
        return (Truncate(code, FacilityCodeMax), Truncate(name, FacilityNameMax));
    }

    public void MergeFrom(PhysicalVolumeImportDraft other)
    {
        Quantity += other.Quantity;
        FacilityCode = PreferNonEmpty(FacilityCode, other.FacilityCode);
        FacilityName = PreferNonEmpty(FacilityName, other.FacilityName);
        BindingType = PreferNonEmpty(BindingType, other.BindingType);
        DocumentSet = PreferNonEmpty(DocumentSet, other.DocumentSet);
        CatalogId = PreferNonEmpty(CatalogId, other.CatalogId);
        Tag = PreferNonEmpty(Tag, other.Tag);
        WorkId = PreferNonEmpty(WorkId, other.WorkId);
        WorkName = PreferNonEmpty(WorkName, other.WorkName);
        WorkComment = PreferNonEmpty(WorkComment, other.WorkComment);
        EstimationBasis = PreferNonEmpty(EstimationBasis, other.EstimationBasis);
        CatalogCode = PreferNonEmpty(CatalogCode, other.CatalogCode);
        CatalogName = PreferNonEmpty(CatalogName, other.CatalogName);
    }

    public void ApplyTruncation()
    {
        FacilityCode = Truncate(FacilityCode, FacilityCodeMax);
        FacilityName = Truncate(FacilityName, FacilityNameMax);
        BindingType = Truncate(BindingType, BindingTypeMax);
        DocumentSet = Truncate(DocumentSet, DocumentSetMax);
        CatalogId = Truncate(CatalogId, CatalogIdMax);
        Tag = Truncate(Tag, TagMax);
        WorkId = Truncate(WorkId, WorkIdMax);
        WorkName = Truncate(WorkName, WorkNameMax);
        WorkComment = Truncate(WorkComment, WorkCommentMax);
        EstimationBasis = Truncate(EstimationBasis, EstimationBasisMax);
        CatalogCode = Truncate(CatalogCode, CatalogCodeMax);
        CatalogName = Truncate(CatalogName, CatalogNameMax);
    }

    public void ApplyTo(ProjectPhysicalVolume entity)
    {
        entity.Quantity = Quantity;
        entity.FacilityCode = FacilityCode;
        entity.FacilityName = FacilityName;
        entity.BindingType = BindingType;
        entity.DocumentSet = DocumentSet;
        entity.CatalogId = CatalogId;
        entity.Tag = Tag;
        entity.WorkId = WorkId;
        entity.WorkName = WorkName;
        entity.WorkComment = WorkComment;
        entity.EstimationBasis = EstimationBasis;
        entity.CatalogCode = CatalogCode;
        entity.CatalogName = CatalogName;
    }

    public ProjectPhysicalVolume ToNewEntity(
        int projectId, int versionId, int physicalVolumeId, bool isKfo, DateTime createdAt, PvCatalogRow catalog)
    {
        var entity = new ProjectPhysicalVolume
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            CreatedAt = createdAt,
            Quantity = Quantity,
            FacilityCode = FacilityCode,
            FacilityName = FacilityName,
            BindingType = BindingType,
            DocumentSet = DocumentSet,
            CatalogId = CatalogId,
            Tag = Tag,
            WorkId = WorkId,
            WorkName = WorkName,
            WorkComment = WorkComment,
            EstimationBasis = EstimationBasis,
            CatalogCode = CatalogCode,
            CatalogName = CatalogName,
        };
        PhysicalVolumeCatalogBinding.AssignCatalogId(entity, physicalVolumeId, isKfo);
        PhysicalVolumeCatalogBinding.FillCatalogSnapshot(
            entity, catalog.Coefficient, catalog.IsAssessment, catalog.ClassifierId);
        return entity;
    }

    private static string? PreferNonEmpty(string? current, string? incoming)
    {
        return string.IsNullOrWhiteSpace(incoming) ? current : incoming;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
