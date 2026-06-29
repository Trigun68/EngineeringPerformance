using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

public partial class IicWorkDirectionNormsWindow : UserControl
{
    public IicWorkDirectionNormsWindow()
    {
        InitializeComponent();
    }

    private void GridControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is IicWorkDirectionNormsViewModel vm && vm.SelectedItem is not null)
        {
            vm.EditItemCommand.Execute(null);
        }
    }

    private void TreeListView_ShowGridMenu(object sender, GridMenuEventArgs e)
    {
        if (e.MenuInfo.MenuType is not (GridMenuType.RowCell or GridMenuType.Column or GridMenuType.GroupPanel))
            return;

        e.MenuInfo.Menu.Items.Add(new BarItemSeparator());
        var expandAll = new BarButtonItem { Content = "Развернуть всё" };
        expandAll.ItemClick += (_, _) => TreeListView.ExpandAllNodes();
        e.MenuInfo.Menu.Items.Add(expandAll);

        var collapseAll = new BarButtonItem { Content = "Свернуть всё" };
        collapseAll.ItemClick += (_, _) => TreeListView.CollapseAllNodes();
        e.MenuInfo.Menu.Items.Add(collapseAll);
    }
}
