using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class PhysicalVolumeKfo : DirectoryItem, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyRowRefreshed()
    {
        var handler = PropertyChanged;
        if (handler is null)
            return;

        void Raise(string name) => handler(this, new PropertyChangedEventArgs(name));

        Raise(nameof(Code));
        Raise(nameof(Name));
        Raise(nameof(Description));
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

    public int? ClassifierId { get; set; }

    public bool? IsAssessment { get; set; }

    public string? DocumentSet { get; set; }

    public decimal Coefficient { get; set; } = 1m;

    public int? UnitId { get; set; }

    public PhysicalVolumeKfo? Parent { get; set; }

    public ICollection<PhysicalVolumeKfo> Children { get; set; } = [];

    public ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes { get; set; } = [];

    public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];

    public IicWorkDirection? Classifier { get; set; }

    [NotMapped]
    public string ClassifierNameWithUnit { get; set; } = string.Empty;

    public UnitOfMeasurement? Unit { get; set; }
}
