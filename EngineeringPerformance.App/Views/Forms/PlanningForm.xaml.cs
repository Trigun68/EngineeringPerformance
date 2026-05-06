using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.TreeList;
using EngineeringPerformance.App.Models;

namespace EngineeringPerformance.App.Views.Forms;

public partial class PlanningForm : UserControl
{
    public PlanningForm()
    {
        InitializeComponent();
    }

    private void PlannedVolumesTreeListView_OnShowingEditor(object sender, TreeListShowingEditorEventArgs e)
    {
        if (e.Column?.FieldName != nameof(ForecastIicVolumeRowModel.PlannedVolume))
        {
            e.Cancel = true;
            return;
        }
    }
}
