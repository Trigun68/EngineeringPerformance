using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.App.Views;

public partial class PhysicalVolumesWindow : UserControl
{
    private int? _draggedItemId;
    private int? _targetParentId;
    private DropPosition _dropPosition;
    private object? _targetRecord;

    public PhysicalVolumesWindow()
    {
        InitializeComponent();
    }

    private void TreeListView_DropRecord(object sender, DropRecordEventArgs e)
    {
        var data = e.Data.GetData(typeof(RecordDragDropData)) as RecordDragDropData;
        if (data is null || !data.Records.Any()) return;

        var draggedItem = data.Records.First() as DirectoryItem;
        if (draggedItem is null) return;

        _draggedItemId = draggedItem.Id;
        _targetParentId = e.TargetRecord is DirectoryItem targetItem ? targetItem.Id : null;
        _dropPosition = e.DropPosition;
        _targetRecord = e.TargetRecord;
    }

    private async void TreeListView_CompleteRecordDragDrop(object sender, CompleteRecordDragDropEventArgs e)
    {
        e.Handled = true;
        if (DataContext is PhysicalVolumesViewModel vm && _draggedItemId.HasValue)
        {
            var sortOrder = CalculateSortOrderIndex(vm.Items, _targetParentId, _dropPosition, _targetRecord);
            await vm.HandleDragDropAsync(_draggedItemId.Value, _targetParentId, sortOrder);
        }

        _draggedItemId = null;
        _targetParentId = null;
        _targetRecord = null;
    }

    private static int CalculateSortOrderIndex(IEnumerable<DirectoryItem> items, int? targetParentId, DropPosition dropPosition, object? targetRecord)
    {
        var siblings = targetParentId.HasValue
            ? items.Where(i => i.ParentId == targetParentId).OrderBy(i => i.SortOrder).ToList()
            : items.Where(i => i.ParentId == null).OrderBy(i => i.SortOrder).ToList();

        if (targetRecord is not DirectoryItem targetItem)
            return siblings.Count;

        var targetIndex = siblings.FindIndex(i => i.Id == targetItem.Id);
        if (targetIndex < 0) return siblings.Count;
        return dropPosition == DropPosition.After ? targetIndex + 1 : targetIndex;
    }

    private void GridControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PhysicalVolumesViewModel vm && vm.SelectedItem is not null)
        {
            vm.EditItemCommand.Execute(null);
        }
    }
}
