import { useCallback, useEffect, useMemo, useRef } from 'react';
import { AgGridReact } from 'ag-grid-react';
import type {
  ColDef,
  ColumnState,
  GetContextMenuItemsParams,
  GridApi,
  GridReadyEvent,
  MenuItemDef,
  RowClassParams,
} from 'ag-grid-community';
import { Box } from '@mui/material';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { getUserSetting, putUserSetting } from '../../api/userSettingsApi';
import { usePermission } from '../../hooks/usePermission';

export interface ContextMenuItem<T = unknown> {
  name: string;
  action: (row: T | undefined) => void;
  disabled?: boolean | ((row: T | undefined) => boolean);
  requiredLevel?: 1 | 2;
  icon?: string;
}

export interface DataTableProps<T> {
  rowData: T[];
  columnDefs: ColDef<T>[];
  objectName: string;
  gridId?: string;
  contextMenuItems?: ContextMenuItem<T>[];
  onRowDoubleClicked?: (row: T) => void;
  onRowClicked?: (row: T) => void;
  onSelectionChanged?: (rows: T[]) => void;
  rowSelectionMode?: 'single' | 'multiple';
  height?: number | string;
  loading?: boolean;
  pagination?: boolean;
  paginationPageSize?: number;
  rowHeight?: number;
  treeData?: boolean;
  getDataPath?: (data: T) => string[];
  autoGroupColumnDef?: ColDef<T>;
  getRowClass?: (params: RowClassParams<T>) => string | string[] | undefined;
  getRowIdValue?: (data: T, index: number) => string;
  gridTestId?: string;
}

const LAYOUT_DEBOUNCE_MS = 400;

export function DataTable<T extends object>({
  rowData,
  columnDefs,
  objectName,
  gridId,
  contextMenuItems = [],
  onRowDoubleClicked,
  onRowClicked,
  onSelectionChanged,
  rowSelectionMode = 'single',
  height = 520,
  loading = false,
  pagination = false,
  paginationPageSize = 50,
  rowHeight = 40,
  treeData = false,
  getDataPath,
  autoGroupColumnDef,
  getRowClass,
  getRowIdValue,
  gridTestId,
}: DataTableProps<T>) {
  const gridRef = useRef<AgGridReact<T>>(null);
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const { canRead, hasLevel } = usePermission(objectName);

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      sortable: true,
      filter: true,
      resizable: true,
      flex: 1,
      minWidth: 120,
    }),
    [],
  );

  const scheduleLayoutSave = useCallback(() => {
    if (!gridId || !gridApiRef.current) return;
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    saveTimerRef.current = setTimeout(() => {
      const state = gridApiRef.current?.getColumnState();
      if (state) {
        void putUserSetting(`grid-${gridId}`, JSON.stringify(state));
      }
    }, LAYOUT_DEBOUNCE_MS);
  }, [gridId]);

  const applySavedLayout = useCallback(async (api: GridApi<T>) => {
    if (!gridId) return;
    const raw = await getUserSetting(`grid-${gridId}`);
    if (!raw) return;
    try {
      const state = JSON.parse(raw) as ColumnState[];
      api.applyColumnState({ state, applyOrder: true });
    } catch {
      // Повреждённый layout — игнорируем.
    }
  }, [gridId]);

  const onGridReady = useCallback((event: GridReadyEvent<T>) => {
    gridApiRef.current = event.api;
    void applySavedLayout(event.api);
  }, [applySavedLayout]);

  useEffect(() => () => {
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
  }, []);

  const getContextMenuItems = useCallback(
    (params: GetContextMenuItemsParams<T>): (string | MenuItemDef)[] => {
      if (!canRead) {
        return ['copy', 'copyWithHeaders'];
      }

      const row = params.node?.data;
      const items: (string | MenuItemDef)[] = contextMenuItems
        .filter((item) => !item.requiredLevel || hasLevel(item.requiredLevel))
        .map((item) => ({
          name: item.name,
          disabled:
            typeof item.disabled === 'function' ? item.disabled(row) : item.disabled,
          icon: item.icon,
          action: () => item.action(row),
        }));

      return [...items, 'separator', 'copy', 'copyWithHeaders', 'export'];
    },
    [canRead, contextMenuItems, hasLevel],
  );

  if (!canRead) {
    return (
      <Box sx={{ p: 2, color: 'text.secondary' }}>
        Нет доступа для просмотра данных.
      </Box>
    );
  }

  return (
    <Box
      className="ag-theme-alpine"
      data-testid={gridTestId}
      sx={{
        width: '100%',
        height,
        minHeight: height === '100%' ? 320 : undefined,
        flex: height === '100%' ? 1 : undefined,
        opacity: loading ? 0.6 : 1,
      }}
    >
      <AgGridReact<T>
        ref={gridRef}
        rowData={rowData}
        columnDefs={columnDefs}
        defaultColDef={defaultColDef}
        animateRows
        rowHeight={rowHeight}
        pagination={pagination}
        paginationPageSize={paginationPageSize}
        rowSelection={rowSelectionMode}
        onSelectionChanged={(event) => {
          if (!onSelectionChanged) return;
          const selected = event.api.getSelectedRows();
          onSelectionChanged(selected);
        }}
        getRowId={(params) =>
          getRowIdValue
            ? getRowIdValue(params.data!, params.data ? rowData.indexOf(params.data) : 0)
            : String((params.data as { id?: number }).id ?? JSON.stringify(params.data))
        }
        getRowClass={getRowClass}
        treeData={treeData}
        getDataPath={getDataPath}
        autoGroupColumnDef={autoGroupColumnDef}
        groupDefaultExpanded={treeData ? -1 : undefined}
        onGridReady={onGridReady}
        onColumnMoved={scheduleLayoutSave}
        onColumnResized={scheduleLayoutSave}
        onColumnVisible={scheduleLayoutSave}
        onSortChanged={scheduleLayoutSave}
        getContextMenuItems={getContextMenuItems}
        onRowClicked={(event) => {
          if (event.data && onRowClicked) {
            onRowClicked(event.data);
          }
        }}
        onRowDoubleClicked={(event) => {
          if (event.data && onRowDoubleClicked) {
            onRowDoubleClicked(event.data);
          }
        }}
        overlayLoadingTemplate='<span class="ag-overlay-loading-center">Загрузка...</span>'
        overlayNoRowsTemplate='<span class="ag-overlay-loading-center">Нет данных</span>'
      />
    </Box>
  );
}
