using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Xpf.Grid;

namespace EngineeringPerformance.App.Services;

/// <summary>
/// Автосохранение раскладки грида (ширина, порядок, видимость столбцов) с debounce.
/// </summary>
internal sealed class GridLayoutAutoSave : IDisposable
{
    private readonly UserSettingsHelper _owner;
    private readonly GridControl _grid;
    private readonly string _gridName;
    private readonly DispatcherTimer _debounceTimer;
    private readonly RoutedEventHandler _endSortingHandler;
    private readonly RoutedEventHandler _endGroupingHandler;
    private readonly MouseButtonEventHandler _mouseLeftButtonUpHandler;
    private readonly DependencyPropertyDescriptor _visibleDpd;
    private readonly DependencyPropertyDescriptor _widthDpd;
    private readonly DependencyPropertyDescriptor _visibleIndexDpd;
    private readonly List<(GridColumn Column, EventHandler Handler)> _columnHandlers = [];

    public string GridName => _gridName;

    public GridLayoutAutoSave(UserSettingsHelper owner, GridControl grid, string gridName)
    {
        _owner = owner;
        _grid = grid;
        _gridName = gridName;

        _debounceTimer = new DispatcherTimer(DispatcherPriority.Background, grid.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(350),
        };
        _debounceTimer.Tick += OnDebounceTick;

        _endSortingHandler = (_, _) => ScheduleSave();
        _endGroupingHandler = (_, _) => ScheduleSave();
        _mouseLeftButtonUpHandler = (_, _) => ScheduleSave();

        _visibleDpd = DependencyPropertyDescriptor.FromProperty(GridColumn.VisibleProperty, typeof(GridColumn));
        _widthDpd = DependencyPropertyDescriptor.FromProperty(GridColumn.WidthProperty, typeof(GridColumn));
        _visibleIndexDpd = DependencyPropertyDescriptor.FromProperty(GridColumn.VisibleIndexProperty, typeof(GridColumn));

        _grid.EndSorting += _endSortingHandler;
        _grid.EndGrouping += _endGroupingHandler;
        _grid.AddHandler(UIElement.MouseLeftButtonUpEvent, _mouseLeftButtonUpHandler, handledEventsToo: true);

        foreach (var col in _grid.Columns)
            AttachColumn(col);
    }

    private void AttachColumn(GridColumn col)
    {
        EventHandler onChanged = (_, _) => ScheduleSave();
        _visibleDpd.AddValueChanged(col, onChanged);
        _widthDpd.AddValueChanged(col, onChanged);
        _visibleIndexDpd.AddValueChanged(col, onChanged);
        _columnHandlers.Add((col, onChanged));
    }

    private void ScheduleSave()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        _ = _owner.SaveGridLayoutAsync(_grid, _gridName);
    }

    /// <summary>Немедленное сохранение при уходе со страницы / закрытии окна.</summary>
    public async Task FlushAsync()
    {
        _debounceTimer.Stop();
        await _owner.SaveGridLayoutAsync(_grid, _gridName);
    }

    /// <summary>
    /// Синхронно останавливает дебаунс-таймер и возвращает XML раскладки.
    /// Должен вызываться на UI-потоке (GridControl требует UI-поток).
    /// Используется в пути закрытия приложения, где недоступен async/await.
    /// </summary>
    public string CollectLayoutXml()
    {
        _debounceTimer.Stop();
        using var ms = new MemoryStream();
        _grid.SaveLayoutToStream(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public void Dispose()
    {
        _debounceTimer.Stop();
        _debounceTimer.Tick -= OnDebounceTick;
        _grid.EndSorting -= _endSortingHandler;
        _grid.EndGrouping -= _endGroupingHandler;
        _grid.RemoveHandler(UIElement.MouseLeftButtonUpEvent, _mouseLeftButtonUpHandler);

        foreach (var (col, handler) in _columnHandlers)
        {
            _visibleDpd.RemoveValueChanged(col, handler);
            _widthDpd.RemoveValueChanged(col, handler);
            _visibleIndexDpd.RemoveValueChanged(col, handler);
        }

        _columnHandlers.Clear();
    }
}
