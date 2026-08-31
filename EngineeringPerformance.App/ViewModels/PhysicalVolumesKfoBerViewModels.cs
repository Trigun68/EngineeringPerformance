using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;

namespace EngineeringPerformance.App.ViewModels;

public sealed class PhysicalVolumesKfoViewModel(
    PhysicalVolumeKfoDirectoryService service,
    IDirectoryCache<PhysicalVolumeKfo> pvCache,
    IDirectoryCache<IicWorkDirection> classifierCache,
    IDirectoryCache<UnitOfMeasurement> unitCache)
    : PhysicalVolumesCatalogViewModelBase<PhysicalVolumeKfo>(service, pvCache, classifierCache, unitCache)
{
    protected override string LoadingTitle => "Загрузка: Номенклатура КФО";
    protected override string ExportSheetTitle => "Номенклатура КФО";
    protected override string ExportFileName => "Номенклатура_КФО.xlsx";

    protected override void ApplyCatalogFields(PhysicalVolumeKfo existing, PhysicalVolumeKfo updated)
    {
        existing.Code = updated.Code;
        existing.Description = updated.Description;
        existing.ClassifierId = updated.ClassifierId;
        existing.Classifier = updated.Classifier;
        existing.ClassifierNameWithUnit = updated.ClassifierNameWithUnit;
        existing.UnitId = updated.UnitId;
        existing.Unit = updated.Unit;
        existing.IsAssessment = updated.IsAssessment;
        existing.DocumentSet = updated.DocumentSet;
        existing.Coefficient = updated.Coefficient;
    }

    protected override void EnrichItem(
        PhysicalVolumeKfo item,
        IReadOnlyDictionary<int, IicWorkDirection> classifiers,
        IReadOnlyDictionary<int, UnitOfMeasurement> units)
    {
        item.Classifier = item.ClassifierId.HasValue && classifiers.TryGetValue(item.ClassifierId.Value, out var c) ? c : null;
        item.Unit = item.UnitId.HasValue && units.TryGetValue(item.UnitId.Value, out var u) ? u : null;
        item.ClassifierNameWithUnit = item.Classifier is { } cl
            ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(cl, classifiers)
            : string.Empty;
    }

    protected override bool MatchesAssessmentFilter(PhysicalVolumeKfo item) =>
        item.IsAssessment == SelectedAssessmentFilter;

    protected override IEnumerable<(string Header, Func<object, object?> Value, int? FixedWidth)> ExportColumns =>
    [
        ("Код", o => ((PhysicalVolumeKfo)o).Code, 15),
        ("Наименование", o => ((PhysicalVolumeKfo)o).Name, 40),
        ("Описание", o => ((PhysicalVolumeKfo)o).Description, 30),
        ("Классификатор", o => ((PhysicalVolumeKfo)o).ClassifierNameWithUnit, 45),
        ("Ед. изм.", o => ((PhysicalVolumeKfo)o).Unit?.Name, null),
        ("Коэффициент", o => ((PhysicalVolumeKfo)o).Coefficient.ToString("G"), null),
        ("Оценка", o => ((PhysicalVolumeKfo)o).IsAssessment?.ToString(), null),
        ("Архив", o => ((PhysicalVolumeKfo)o).IsArchived.ToString(), null),
    ];
}

public sealed class PhysicalVolumesBerViewModel(
    PhysicalVolumeBerDirectoryService service,
    IDirectoryCache<PhysicalVolumeBer> pvCache,
    IDirectoryCache<IicWorkDirection> classifierCache,
    IDirectoryCache<UnitOfMeasurement> unitCache)
    : PhysicalVolumesCatalogViewModelBase<PhysicalVolumeBer>(service, pvCache, classifierCache, unitCache)
{
    protected override string LoadingTitle => "Загрузка: Номенклатура БЕР";
    protected override string ExportSheetTitle => "Номенклатура БЕР";
    protected override string ExportFileName => "Номенклатура_БЕР.xlsx";

    protected override void ApplyCatalogFields(PhysicalVolumeBer existing, PhysicalVolumeBer updated)
    {
        existing.Code = updated.Code;
        existing.Description = updated.Description;
        existing.ClassifierId = updated.ClassifierId;
        existing.Classifier = updated.Classifier;
        existing.ClassifierNameWithUnit = updated.ClassifierNameWithUnit;
        existing.UnitId = updated.UnitId;
        existing.Unit = updated.Unit;
        existing.IsAssessment = updated.IsAssessment;
        existing.DocumentSet = updated.DocumentSet;
        existing.Coefficient = updated.Coefficient;
    }

    protected override void EnrichItem(
        PhysicalVolumeBer item,
        IReadOnlyDictionary<int, IicWorkDirection> classifiers,
        IReadOnlyDictionary<int, UnitOfMeasurement> units)
    {
        item.Classifier = item.ClassifierId.HasValue && classifiers.TryGetValue(item.ClassifierId.Value, out var c) ? c : null;
        item.Unit = item.UnitId.HasValue && units.TryGetValue(item.UnitId.Value, out var u) ? u : null;
        item.ClassifierNameWithUnit = item.Classifier is { } cl
            ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(cl, classifiers)
            : string.Empty;
    }

    protected override bool MatchesAssessmentFilter(PhysicalVolumeBer item) =>
        item.IsAssessment == SelectedAssessmentFilter;

    protected override IEnumerable<(string Header, Func<object, object?> Value, int? FixedWidth)> ExportColumns =>
    [
        ("Код", o => ((PhysicalVolumeBer)o).Code, 15),
        ("Наименование", o => ((PhysicalVolumeBer)o).Name, 40),
        ("Описание", o => ((PhysicalVolumeBer)o).Description, 30),
        ("Классификатор", o => ((PhysicalVolumeBer)o).ClassifierNameWithUnit, 45),
        ("Ед. изм.", o => ((PhysicalVolumeBer)o).Unit?.Name, null),
        ("Коэффициент", o => ((PhysicalVolumeBer)o).Coefficient.ToString("G"), null),
        ("Оценка", o => ((PhysicalVolumeBer)o).IsAssessment?.ToString(), null),
        ("Архив", o => ((PhysicalVolumeBer)o).IsArchived.ToString(), null),
    ];
}
