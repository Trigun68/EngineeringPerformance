using DevExpress.Xpf.Grid;

namespace EngineeringPerformance.App.Localization;

/// <summary>
/// Русские подписи для встроенного контекстного меню GridControl (заголовок столбца,
/// панель групп, подвал с итогами и т.д.). См. DevExpress: GridControlLocalizer, GridControlStringId.
/// </summary>
internal sealed class RussianGridControlLocalizer : GridControlLocalizer
{
    protected override void PopulateStringTable()
    {
        base.PopulateStringTable();

        AddString(GridControlStringId.GridGroupPanelText,
            "Перетащите заголовок столбца сюда, чтобы сгруппировать по этому столбцу");

        AddString(GridControlStringId.MenuGroupPanelFullExpand, "Развернуть все группы");
        AddString(GridControlStringId.MenuGroupPanelFullCollapse, "Свернуть все группы");
        AddString(GridControlStringId.MenuGroupPanelClearGrouping, "Очистить группировку");

        AddString(GridControlStringId.MenuColumnSortAscending, "Сортировать по возрастанию");
        AddString(GridControlStringId.MenuColumnSortDescending, "Сортировать по убыванию");
        AddString(GridControlStringId.MenuColumnClearSorting, "Сбросить сортировку");

        AddString(GridControlStringId.MenuColumnSortBySummaryAscending, "По возрастанию");
        AddString(GridControlStringId.MenuColumnSortBySummaryDescending, "По убыванию");
        AddString(GridControlStringId.MenuColumnSortBySummaryMax, "Максимум");
        AddString(GridControlStringId.MenuColumnSortBySummaryMin, "Минимум");
        AddString(GridControlStringId.MenuColumnSortBySummaryCount, "Количество");
        AddString(GridControlStringId.MenuColumnSortBySummaryAverage, "Среднее");
        AddString(GridControlStringId.MenuColumnSortBySummarySum, "Сумма");
        AddString(GridControlStringId.MenuColumnResetGroupSummarySort, "Сбросить сортировку по итогу");
        AddString(GridControlStringId.MenuColumnSortGroupBySummaryMenu, "Сортировать по групповому итогу");
        AddString(GridControlStringId.MenuColumnGroupSummarySortFormat, "Сортировать по итогу ({0}) — {1}: {2}");
        AddString(GridControlStringId.MenuColumnGroupSummaryEditor, "Редактор групповых итогов…");

        AddString(GridControlStringId.MenuColumnGroup, "Группировать по этому столбцу");
        AddString(GridControlStringId.MenuColumnUnGroup, "Разгруппировать");
        AddString(GridControlStringId.MenuColumnShowGroupPanel, "Показать панель группировки");
        AddString(GridControlStringId.MenuColumnHideGroupPanel, "Скрыть панель группировки");

        AddString(GridControlStringId.MenuColumnGroupInterval, "Интервал группировки");
        AddString(GridControlStringId.MenuColumnGroupIntervalNone, "Нет");
        AddString(GridControlStringId.MenuColumnGroupIntervalDay, "По дням");
        AddString(GridControlStringId.MenuColumnGroupIntervalMonth, "По месяцам");
        AddString(GridControlStringId.MenuColumnGroupIntervalYear, "По годам");
        AddString(GridControlStringId.MenuColumnGroupIntervalSmart, "Умный интервал");

        AddString(GridControlStringId.MenuColumnShowColumnChooser, "Выбор столбцов…");
        AddString(GridControlStringId.MenuColumnHideColumnChooser, "Скрыть выбор столбцов");
        AddString(GridControlStringId.MenuColumnShowColumnBandChooser, "Выбор диапазонов столбцов…");
        AddString(GridControlStringId.MenuColumnHideColumnBandChooser, "Скрыть выбор диапазонов");

        AddString(GridControlStringId.MenuColumnBestFit, "Наилучшая ширина столбца");
        AddString(GridControlStringId.MenuColumnBestFitColumns, "Наилучшая ширина всех столбцов");

        AddString(GridControlStringId.MenuColumnClearFilter, "Очистить фильтр");
        AddString(GridControlStringId.MenuColumnFilterEditor, "Редактор фильтра…");
        AddString(GridControlStringId.FilterEditorTitle, "Редактор фильтра");

        AddString(GridControlStringId.MenuColumnFixedStyle, "Закрепление");
        AddString(GridControlStringId.MenuColumnFixedNone, "Не закреплять");
        AddString(GridControlStringId.MenuColumnFixedLeft, "Закрепить слева");
        AddString(GridControlStringId.MenuColumnFixedRight, "Закрепить справа");

        AddString(GridControlStringId.MenuColumnShowSearchPanel, "Показать строку поиска");
        AddString(GridControlStringId.MenuColumnHideSearchPanel, "Скрыть строку поиска");

        AddString(GridControlStringId.MenuColumnUnboundExpressionEditor, "Редактор выражения…");

        AddString(GridControlStringId.MenuFooterSum, "Сумма");
        AddString(GridControlStringId.MenuFooterMin, "Минимум");
        AddString(GridControlStringId.MenuFooterMax, "Максимум");
        AddString(GridControlStringId.MenuFooterCount, "Количество");
        AddString(GridControlStringId.MenuFooterAverage, "Среднее");
        AddString(GridControlStringId.MenuFooterCustom, "Пользовательский…");
        AddString(GridControlStringId.MenuFooterCustomize, "Настроить итоги…");
        AddString(GridControlStringId.MenuFooterRowCount, "Число строк");

        AddString(GridControlStringId.ColumnChooserCaption, "Выбор столбцов");
        AddString(GridControlStringId.ColumnChooserDragText, "Перетащите заголовок сюда, чтобы скрыть столбец");

        // Пункты «Условное форматирование» в GridControlStringId помечены Obsolete — в v25.2 используйте
        // DevExpress.Xpf.Core.ConditionalFormatting.ConditionalFormattingLocalizer (отдельный NuGet при необходимости).

        AddString(GridControlStringId.MenuRowFixedTop, "Закрепить строку сверху");
        AddString(GridControlStringId.MenuRowFixedNone, "Не закреплять строку");
        AddString(GridControlStringId.MenuRowFixedBottom, "Закрепить строку снизу");
    }
}
