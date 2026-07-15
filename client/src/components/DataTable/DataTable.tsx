import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { AgGridReact } from 'ag-grid-react';
import type {
  ColDef,
  Column,
  ColumnState,
  FilterChangedEvent,
  GetContextMenuItemsParams,
  GridApi,
  GridReadyEvent,
  MenuItemDef,
  RowClassParams,
} from 'ag-grid-community';
import {
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import ViewColumnOutlinedIcon from '@mui/icons-material/ViewColumnOutlined';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { getUserSetting, putUserSetting } from '../../api/userSettingsApi';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import { usePermission } from '../../hooks/usePermission';
import { wpColors } from '../../theme/wpColors';
import { agGridLocaleRu } from '../../utils/agGridLocaleRu';
import { GridViewport } from '../dictionary/GridViewport';
import { dataTableAggFuncs } from './dataTableAggFuncs';

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
  rowGroupPanelShow?: 'always' | 'onlyWhenGrouping' | 'never';
  enableRowGrouping?: boolean;
  showColumnSettings?: boolean;
  pinnedBottomRowData?: T[];
  gridTestId?: string;
}

const LAYOUT_DEBOUNCE_MS = 400;
const FILTER_DEBOUNCE_MS = 800;

function layoutPrefKey(gridId: string): string {
  return `grid.${gridId}.layout`;
}

function filterPrefKey(gridId: string): string {
  return `grid.${gridId}.filter`;
}

function sanitizeFilterModel(model: Record<string, unknown>): Record<string, unknown> | null {
  if (Object.keys(model).length === 0) return null;
  return model;
}

function columnLabel(col: Column): string {
  const def = col.getColDef();
  return def.headerName ?? def.field ?? col.getColId();
}

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
  height = '100%',
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
  rowGroupPanelShow,
  enableRowGrouping = false,
  showColumnSettings = false,
  pinnedBottomRowData,
}: DataTableProps<T>) {
  const gridRef = useRef<AgGridReact<T>>(null);
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const filterTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const gridInitializedRef = useRef(false);
  const isFirstRenderRef = useRef(true);
  const filterAppliedRef = useRef(false);
  const { canRead, hasLevel } = usePermission(objectName);

  const [columnConfigOpen, setColumnConfigOpen] = useState(false);
  const [columnVisibility, setColumnVisibility] = useState<Record<string, boolean>>({});
  const [allColumnLabels, setAllColumnLabels] = useState<Record<string, string>>({});

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      sortable: true,
      filter: true,
      resizable: true,
      flex: 1,
      minWidth: 120,
      headerTooltipValueGetter: (params: { colDef: ColDef<T> }) =>
        params.colDef.headerTooltip ?? params.colDef.headerName ?? params.colDef.field ?? '',
    }),
    [],
  );

  const persistLayoutNow = useCallback(async () => {
    const api = gridApiRef.current;
    if (!api || !gridId || !gridInitializedRef.current) return;
    const layout = api.getColumnState();
    await putUserPreferences({ [layoutPrefKey(gridId)]: JSON.stringify(layout) });
    // Обратная совместимость со старым ключом.
    await putUserSetting(`grid-${gridId}`, JSON.stringify(layout));
  }, [gridId]);

  const scheduleLayoutSave = useCallback(() => {
    if (!gridId || !gridApiRef.current || !gridInitializedRef.current) return;
    if (isFirstRenderRef.current) return;
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    saveTimerRef.current = setTimeout(() => {
      void persistLayoutNow();
    }, LAYOUT_DEBOUNCE_MS);
  }, [gridId, persistLayoutNow]);

  const scheduleFilterSave = useCallback(() => {
    if (!gridId || !gridApiRef.current || !gridInitializedRef.current) return;
    if (isFirstRenderRef.current) return;
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
    filterTimerRef.current = setTimeout(() => {
      const api = gridApiRef.current;
      if (!api || !gridId) return;
      const filter = api.getFilterModel() as Record<string, unknown>;
      const sanitized = sanitizeFilterModel(filter);
      void putUserPreferences({
        [filterPrefKey(gridId)]: JSON.stringify(sanitized ?? {}),
      });
    }, FILTER_DEBOUNCE_MS);
  }, [gridId]);

  const applySavedLayout = useCallback(async (api: GridApi<T>) => {
    if (!gridId) return;
    let raw: string | undefined;
    try {
      const prefs = await getUserPreferences();
      raw = prefs[layoutPrefKey(gridId)];
    } catch {
      raw = undefined;
    }
    if (!raw) {
      raw = (await getUserSetting(`grid-${gridId}`)) ?? undefined;
    }
    if (!raw) return;
    try {
      const state = JSON.parse(raw) as ColumnState[];
      api.applyColumnState({ state, applyOrder: true });
    } catch {
      // Повреждённый layout — игнорируем.
    }
  }, [gridId]);

  const applySavedFilters = useCallback(async (api: GridApi<T>) => {
    if (!gridId || filterAppliedRef.current) return;
    try {
      const prefs = await getUserPreferences();
      const raw = prefs[filterPrefKey(gridId)];
      if (!raw) return;
      const model = JSON.parse(raw) as Record<string, unknown>;
      if (Object.keys(model).length > 0) {
        api.setFilterModel(model);
      }
      filterAppliedRef.current = true;
    } catch {
      // ignore
    }
  }, [gridId]);

  const syncColumnVisibilityState = useCallback((api: GridApi<T>) => {
    const labels: Record<string, string> = {};
    const visibility: Record<string, boolean> = {};
    api.getColumns()?.forEach((col) => {
      const id = col.getColId();
      labels[id] = columnLabel(col);
      visibility[id] = col.isVisible();
    });
    setAllColumnLabels(labels);
    setColumnVisibility(visibility);
  }, []);

  const onGridReady = useCallback((event: GridReadyEvent<T>) => {
    gridApiRef.current = event.api;
    void applySavedLayout(event.api).then(() => {
      syncColumnVisibilityState(event.api);
      gridInitializedRef.current = true;
      isFirstRenderRef.current = false;
    });
  }, [applySavedLayout, syncColumnVisibilityState]);

  useEffect(() => {
    const api = gridApiRef.current;
    if (!api || !gridInitializedRef.current || rowData.length === 0) return;
    const timer = setTimeout(() => {
      void applySavedFilters(api);
    }, 300);
    return () => clearTimeout(timer);
  }, [applySavedFilters, rowData.length]);

  const onColumnRowGroupChanged = useCallback(() => {
    scheduleLayoutSave();
  }, [scheduleLayoutSave]);

  const onFilterChanged = useCallback(
    (_event: FilterChangedEvent<T>) => {
      scheduleFilterSave();
    },
    [scheduleFilterSave],
  );

  useEffect(() => {
    const api = gridApiRef.current;
    if (!api || !gridInitializedRef.current) return;
    void applySavedLayout(api).then(() => syncColumnVisibilityState(api));
  }, [applySavedLayout, columnDefs, syncColumnVisibilityState]);

  useEffect(() => () => {
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
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

  const openColumnConfig = () => {
    const api = gridApiRef.current;
    if (api) syncColumnVisibilityState(api);
    setColumnConfigOpen(true);
  };

  const applyColumnVisibility = () => {
    const api = gridApiRef.current;
    if (!api) return;
    const state = api.getColumnState().map((col) => ({
      ...col,
      hide: columnVisibility[col.colId] === false,
    }));
    api.applyColumnState({ state, applyOrder: true });
    setColumnConfigOpen(false);
    scheduleLayoutSave();
  };

  const configurableColumnIds = useMemo(
    () => Object.keys(allColumnLabels),
    [allColumnLabels],
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
      data-testid={gridTestId}
      sx={{
        width: '100%',
        height,
        minHeight: height === '100%' ? 0 : undefined,
        flex: height === '100%' ? 1 : undefined,
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
      }}
    >
      {showColumnSettings && (
        <Stack direction="row" justifyContent="flex-end" sx={{ mb: 0.5, flexShrink: 0 }}>
          <Tooltip title="Настроить столбцы">
            <IconButton size="small" onClick={openColumnConfig} aria-label="Настроить столбцы">
              <ViewColumnOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Stack>
      )}

      <GridViewport loading={loading}>
        <AgGridReact<T>
          containerStyle={{ height: '100%', width: '100%' }}
          ref={gridRef}
          rowData={rowData}
          columnDefs={columnDefs}
          defaultColDef={defaultColDef}
          localeText={agGridLocaleRu}
          domLayout="normal"
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
          rowGroupPanelShow={enableRowGrouping ? (rowGroupPanelShow ?? 'always') : undefined}
          groupDisplayType={enableRowGrouping ? 'multipleColumns' : undefined}
          groupIncludeFooter={enableRowGrouping}
          groupIncludeTotalFooter={false}
          suppressAggFuncInHeader={enableRowGrouping}
          grandTotalRow={enableRowGrouping ? 'bottom' : undefined}
          aggFuncs={dataTableAggFuncs}
          pinnedBottomRowData={pinnedBottomRowData}
          onGridReady={onGridReady}
          onColumnMoved={scheduleLayoutSave}
          onColumnResized={scheduleLayoutSave}
          onColumnVisible={scheduleLayoutSave}
          onSortChanged={scheduleLayoutSave}
          onColumnRowGroupChanged={onColumnRowGroupChanged}
          onFilterChanged={onFilterChanged}
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
      </GridViewport>

      <Dialog open={columnConfigOpen} onClose={() => setColumnConfigOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ color: wpColors.heading }}>Настроить столбцы</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={0.5}>
            {configurableColumnIds.map((colId) => (
              <FormControlLabel
                key={colId}
                control={
                  <Checkbox
                    size="small"
                    checked={columnVisibility[colId] !== false}
                    onChange={(e) =>
                      setColumnVisibility((prev) => ({
                        ...prev,
                        [colId]: e.target.checked,
                      }))
                    }
                  />
                }
                label={
                  <Typography variant="body2">{allColumnLabels[colId] ?? colId}</Typography>
                }
              />
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setColumnConfigOpen(false)}>Отмена</Button>
          <Button variant="contained" onClick={applyColumnVisibility}>
            Применить
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
