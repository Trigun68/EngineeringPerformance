using System.ComponentModel;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class PhysicalVolumeType : DirectoryItem, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// После слияния строки из БД — иначе грид не обновляет «Код», «Наименование» и др.
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
        Raise(nameof(ParentId));
        Raise(nameof(SortOrder));
        Raise(nameof(IsArchived));
        Raise(nameof(CreatedAt));
        Raise(nameof(ModifiedAt));
    }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
