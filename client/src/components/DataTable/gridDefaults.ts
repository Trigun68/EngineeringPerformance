import type { ColDef } from 'ag-grid-community';

import { agGridLocaleRu } from '../../utils/agGridLocaleRu';

/**
 * Общие настройки всех AG Grid-таблиц проекта (DataTable, DictionaryGrid,
 * PermissionMatrixGrid). Единая точка, чтобы не разводить «второй стек»
 * с расходящимися локалью, оверлеями и базовыми ColDef.
 */
export { agGridLocaleRu };

export const GRID_OVERLAY_LOADING_TEMPLATE =
  '<span class="ag-overlay-loading-center">Загрузка...</span>';

export const GRID_OVERLAY_NO_ROWS_TEMPLATE =
  '<span class="ag-overlay-loading-center">Нет данных</span>';

/**
 * Базовый ColDef: сортировка/ресайз/minWidth. Специализированные гриды
 * расширяют его своим filter / headerTooltipValueGetter.
 */
export function createBaseColDef<T>(): ColDef<T> {
  return {
    sortable: true,
    resizable: true,
    minWidth: 80,
  };
}
