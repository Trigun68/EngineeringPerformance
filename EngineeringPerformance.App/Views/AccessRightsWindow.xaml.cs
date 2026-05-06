using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Data;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.App.Views;

public partial class AccessRightsWindow : UserControl
{
    public AccessRightsWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AccessRightsViewModel vm)
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(AccessRightsViewModel.RoleNames))
                    RebuildRoleColumns(vm);
            };
    }

    private void RebuildRoleColumns(AccessRightsViewModel vm)
    {
        while (Grid.Columns.Count > 1)
            Grid.Columns.RemoveAt(Grid.Columns.Count - 1);

        foreach (var roleName in vm.RoleNames)
        {
            Grid.Columns.Add(new GridColumn
            {
                Header = roleName,
                FieldName = roleName,
                Width = 160,
                UnboundType = UnboundColumnType.Object,
                AllowEditing = DevExpress.Utils.DefaultBoolean.True,
                EditSettings = new ComboBoxEditSettings
                {
                    ItemsSource = vm.AccessLevelOptions,
                    ValueMember = "Value",
                    DisplayMember = "Display",
                },
            });
        }
    }

    private void Grid_CustomUnboundColumnData(object sender, GridColumnDataEventArgs e)
    {
        if (DataContext is not AccessRightsViewModel vm) return;
        if (e.ListSourceRowIndex < 0 || e.ListSourceRowIndex >= vm.Rows.Count) return;

        var row = vm.Rows[e.ListSourceRowIndex];
        var cell = row.GetCell(e.Column.FieldName);
        if (cell is null) return;

        if (e.IsGetData)
            e.Value = cell.AccessLevel;
        else if (e.IsSetData && e.Value is AccessLevel level)
            cell.AccessLevel = level;
    }

    private async void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AccessRightsViewModel vm)
            await vm.SaveAsync();
    }
}
