using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.App.Views;

public partial class ProjectAnalysisVersionsWindow : UserControl
{
    private int? _draggedItemId;
    private int _targetIndex;

    public ProjectAnalysisVersionsWindow()
    {
        InitializeComponent();
    }

    private void TableView_DropRecord(object sender, DropRecordEventArgs e)
    {
        var data = e.Data.GetData(typeof(RecordDragDropData)) as RecordDragDropData;
        if (data is null || !data.Records.Any()) return;

        var draggedItem = data.Records.First() as DirectoryItem;
        if (draggedItem is null) return;

        _draggedItemId = draggedItem.Id;

        if (DataContext is ProjectAnalysisVersionsViewModel vm)
        {
            _targetIndex = CalculateSortOrderIndex(vm.Items, e);
        }
    }

    private static int CalculateSortOrderIndex(IEnumerable<DirectoryItem> items, DropRecordEventArgs e)
    {
        var list = items.ToList();
        if (e.TargetRecord is not DirectoryItem targetItem)
        {
            return list.Count;
        }

        var targetIndex = list.FindIndex(i => i.Id == targetItem.Id);
        if (targetIndex < 0) return list.Count;
        return e.DropPosition == DropPosition.After ? targetIndex + 1 : targetIndex;
    }

    private async void TableView_CompleteRecordDragDrop(object sender, CompleteRecordDragDropEventArgs e)
    {
        e.Handled = true;
        if (DataContext is ProjectAnalysisVersionsViewModel vm && _draggedItemId.HasValue)
        {
            await vm.HandleDragDropAsync(_draggedItemId.Value, null, _targetIndex);
        }

        _draggedItemId = null;
        _targetIndex = 0;
    }

    private void GridControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProjectAnalysisVersionsViewModel vm && vm.SelectedItem is not null)
        {
            vm.EditItemCommand.Execute(null);
        }
    }
}
