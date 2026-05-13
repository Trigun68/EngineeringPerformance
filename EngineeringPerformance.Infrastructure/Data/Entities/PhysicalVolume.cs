using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class PhysicalVolume : DirectoryItem, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// После слияния строки из БД в уже показанном гриде — иначе вложенные пути вроде
    /// <c>VolumeType.Name</c> остаются на старой ссылке навигации.
    /// </summary>
    public void NotifyRowRefreshed()
    {
        var handler = PropertyChanged;
        if (handler is null)
        {
            return;
        }

        void Raise(string name) => handler(this, new PropertyChangedEventArgs(name));

        Raise(nameof(Code));
        Raise(nameof(Name));
        Raise(nameof(Description));
        Raise(nameof(VolumeTypeId));
        Raise(nameof(VolumeType));
        Raise(nameof(ClassifierId));
        Raise(nameof(Classifier));
        Raise(nameof(ClassifierNameWithUnit));
        Raise(nameof(IsAssessment));
        Raise(nameof(Coefficient));
        Raise(nameof(DocumentSet));
        Raise(nameof(UnitId));
        Raise(nameof(Unit));
        Raise(nameof(ParentId));
        Raise(nameof(SortOrder));
        Raise(nameof(IsArchived));
        Raise(nameof(CreatedAt));
        Raise(nameof(ModifiedAt));
    }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int VolumeTypeId { get; set; }

    public int? ClassifierId { get; set; }

    public bool? IsAssessment { get; set; }

    public string? DocumentSet { get; set; }

    /// <summary>Переводит количество по проекту в единицы измерения группы физ. объёмов (ИИС).</summary>
    public decimal Coefficient { get; set; } = 1m;

    /// <summary>Единица измерения из исходного справочника (БЭР24/КФО2). FK на UnitsOfMeasurement.</summary>
    public int? UnitId { get; set; }

    public PhysicalVolume? Parent { get; set; }

    public ICollection<PhysicalVolume> Children { get; set; } = [];

    public ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public PhysicalVolumeType VolumeType { get; set; } = null!;

    public IicWorkDirection? Classifier { get; set; }

    /// <summary>Классификатор для грида: наименование группы ИИС и ед. изм. по правилам справочника ИИС.</summary>
    [NotMapped]
    public string ClassifierNameWithUnit { get; set; } = string.Empty;

    public UnitOfMeasurement? Unit { get; set; }
}
