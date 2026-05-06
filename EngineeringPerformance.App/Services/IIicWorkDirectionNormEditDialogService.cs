using System.Windows;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.App.Services;

public interface IIicWorkDirectionNormEditDialogService
{
    void SetOwnerWindow(Window owner);
    Task<bool> ShowEditDialogAsync(IicWorkDirectionNorm? item = null, int? preselectedWorkDirectionId = null);
}
