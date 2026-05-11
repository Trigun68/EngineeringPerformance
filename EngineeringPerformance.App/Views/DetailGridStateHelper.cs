using DevExpress.Xpf.Grid;
using EngineeringPerformance.App.ViewModels;

namespace EngineeringPerformance.App.Views;

// Сохранение/восстановление состояния детальной сетки прогноза/факта.
// DevExpress сам через RestoreStateOnSourceChange + RestoreStateKeyFieldName в XAML
// возвращает focus и раскрытие групп; здесь возвращается только позиция вертикального
// скролла, которую DevExpress не сохраняет при смене ItemsSource.
internal static class DetailGridStateHelper
{
    public static DetailGridState Save(GridControl grid)
    {
        var tv = grid.View as TableView;
        var state = new DetailGridState();
        var topIndex = tv?.TopRowIndex ?? 0;
        state.TopRowIndex = topIndex;
        if (tv is not null)
        {
            var focusedRh = tv.FocusedRowHandle;
            if (!grid.IsGroupRowHandle(focusedRh) && grid.GetRow(focusedRh) is DetailForecastItem focused)
            {
                state.FocusedRowId = focused.ProjectPhysicalVolumeId;
                for (var i = 0; i < grid.VisibleRowCount; i++)
                {
                    if (grid.GetRowHandleByVisibleIndex(i) == focusedRh)
                    {
                        state.FocusedVisibleOffset = i - topIndex;
                        break;
                    }
                }
            }
        }
        return state;
    }

    public static void Restore(GridControl grid, DetailGridState state)
    {
        if (grid.View is not TableView tv) return;

        var visibleCount = grid.VisibleRowCount;
        if (visibleCount <= 0)
        {
            tv.TopRowIndex = 0;
            return;
        }

        if (state.FocusedRowId >= 0)
        {
            for (var i = 0; i < visibleCount; i++)
            {
                var rh = grid.GetRowHandleByVisibleIndex(i);
                if (!grid.IsGroupRowHandle(rh) && grid.GetRow(rh) is DetailForecastItem row
                    && row.ProjectPhysicalVolumeId == state.FocusedRowId)
                {
                    tv.TopRowIndex = Math.Max(0, i - state.FocusedVisibleOffset);
                    return;
                }
            }
        }

        // Фокусной строки нет в новом снимке (удалена/отфильтрована/перешла под другую группу) —
        // откатываемся на сохранённый абсолютный TopRowIndex с clamp-ом к актуальному количеству строк.
        tv.TopRowIndex = Math.Clamp(state.TopRowIndex, 0, visibleCount - 1);
    }
}
