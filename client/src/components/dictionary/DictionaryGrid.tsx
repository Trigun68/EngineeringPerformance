import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
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
import { ExportActionButton } from '../export/ExportActionButton';
import { ToolbarActionButton } from '../export/ToolbarActionButton';
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
} from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import type { PermissionObjectName } from '../../constants/accessControlConstants';
import { usePermission } from '../../hooks/usePermission';
import { wpColors } from '../../theme/wpColors';
import {
  agGridLocaleRu,
  GRID_OVERLAY_LOADING_TEMPLATE,
  GRID_OVERLAY_NO_ROWS_TEMPLATE,
} from '../DataTable/gridDefaults';
import { emptyRows } from '../DataTable/emptyRows';
import {
  filterPrefKey,
  loadGridLayout,
  persistGridLayout,
  setCachedGridLayout,
} from '../../utils/gridLayoutPersistence';
import { ExcelSetColumnFilter } from './ExcelSetColumnFilter';
import { GridViewport } from './GridViewport';

export interface DictionaryContextMenuItem<T> {
  name: string;
  action: (row: T | undefined) => void;
  disabled?: boolean | ((row: T | undefined) => boolean);
  requiredLevel?: 1 | 2;
  icon?: string;
}

export interface DictionaryGridProps<T extends { id: number }> {
  rowData: T[];
  columnDefs: ColDef<T>[];
  objectName: PermissionObjectName;
  gridId: string;
  loading?: boolean;
  treeData?: boolean;
  /** Поле с массивом пути для treeData (например, `hierarchy`). */
  hierarchyField?: keyof T;
  getDataPath?: (data: T) => string[];
  autoGroupColumnDef?: ColDef<T>;
  canEdit?: boolean;
  onEdit?: (row: T) => void;
  onArchive?: (row: T) => void;
  onDelete?: (row: T) => void;
  onRowClicked?: (row: T) => void;
  onRowDoubleClicked?: (row: T) => void;
  onExportExcel?: () => void;
  showExportButton?: boolean;
  onGridApiReady?: (api: GridApi<T>) => void;
  additionalContextMenuItems?: DictionaryContextMenuItem<T>[];
  /** compact — только «Копировать», без export и copyWithHeaders */
  contextMenuMode?: 'full' | 'compact';
}

const LAYOUT_DEBOUNCE_MS = 1000;
const FILTER_DEBOUNCE_MS = 800;

const SET_FILTER_PARAMS = {
  buttons: ['apply', 'reset'] as const,
  closeOnApply: true,
  suppressSorting: true,
  excelMode: 'windows' as const,
};

/** Пустой set-filter (values: []) скрывает все строки — не восстанавливаем и не сохраняем. */
function isBlockingEmptySetFilter(model: Record<string, unknown>): boolean {
  return Object.values(model).some((value) => {
    if (!value || typeof value !== 'object' || !('values' in value)) return false;
    const values = (value as { values?: unknown[] }).values;
    return Array.isArray(values) && values.length === 0;
  });
}

function sanitizeFilterModel(model: Record<string, unknown>): Record<string, unknown> | null {
  if (Object.keys(model).length === 0 || isBlockingEmptySetFilter(model)) {
    return null;
  }
  return model;
}

function columnLabel(col: Column): string {
  const def = col.getColDef();
  return def.headerName ?? def.field ?? col.getColId();
}

function DictionaryGridInternal<T extends { id: number }>({
  rowData,
  columnDefs,
  objectName,
  gridId,
  loading = false,
  treeData = false,
  hierarchyField,
  getDataPath: getDataPathProp,
  autoGroupColumnDef,
  canEdit: canEditProp,
  onEdit,
  onArchive,
  onDelete,
  onRowClicked,
  onRowDoubleClicked,
  onExportExcel,
  showExportButton = false,
  onGridApiReady,
  additionalContextMenuItems = [],
  contextMenuMode = 'full',
}: DictionaryGridProps<T>) {
  const resolvedRowData = rowData.length === 0 ? emptyRows<T>() : rowData;
  const gridRef = useRef<AgGridReact<T>>(null);
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const gridInitializedRef = useRef(false);
  const isFirstRenderRef = useRef(true);
  const filterAppliedRef = useRef(false);
  const lastLayoutRef = useRef<ColumnState[] | null>(null);

  const { canRead, canEdit: canEditPermission, hasLevel } = usePermission(objectName);
  const canEdit = canEditProp ?? canEditPermission;

  const [columnConfigOpen, setColumnConfigOpen] = useState(false);
  const [columnVisibility, setColumnVisibility] = useState<Record<string, boolean>>({});
  const [allColumnLabels, setAllColumnLabels] = useState<Record<string, string>>({});

  const seedColumnMetaFromDefs = useCallback(() => {
    const labels: Record<string, string> = {};
    const visibility: Record<string, boolean> = {};

    columnDefs.forEach((col, index) => {
      const colId = col.colId ?? col.field ?? col.headerName ?? `col_${index}`;
      labels[colId] = col.headerName ?? String(colId);
      visibility[colId] = col.hide !== true;
    });

    if (autoGroupColumnDef) {
      const groupColId =
        autoGroupColumnDef.colId ?? autoGroupColumnDef.field ?? 'ag-Grid-AutoColumn';
      labels[groupColId] = autoGroupColumnDef.headerName ?? groupColId;
      visibility[groupColId] = autoGroupColumnDef.hide !== true;
    }

    return { labels, visibility };
  }, [autoGroupColumnDef, columnDefs]);

  const resolvedGetDataPath = useCallback(
    (data: T): string[] => {
      if (getDataPathProp) return getDataPathProp(data);
      if (hierarchyField) {
        const value = data[hierarchyField];
        return Array.isArray(value) ? (value as string[]) : [];
      }
      return [];
    },
    [getDataPathProp, hierarchyField],
  );

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      sortable: true,
      filter: ExcelSetColumnFilter,
      filterParams: SET_FILTER_PARAMS,
      resizable: true,
      minWidth: 80,
    }),
    [],
  );

  const captureLayoutFromApi = useCallback((): ColumnState[] | null => {
    const api = gridApiRef.current;
    if (!api) return lastLayoutRef.current;
    const layout = api.getColumnState();
    lastLayoutRef.current = layout;
    setCachedGridLayout(gridId, layout);
    return layout;
  }, [gridId]);

  const persistLayoutNow = useCallback(async () => {
    if (!gridInitializedRef.current) return;
    const layout = captureLayoutFromApi() ?? lastLayoutRef.current;
    if (!layout || layout.length === 0) return;
    await persistGridLayout(gridId, layout, { legacyUserSetting: false });
  }, [captureLayoutFromApi, gridId]);

  const scheduleLayoutSave = useCallback(() => {
    if (!gridInitializedRef.current) return;
    if (isFirstRenderRef.current) return;
    captureLayoutFromApi();
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    saveTimerRef.current = setTimeout(() => {
      void persistLayoutNow();
    }, LAYOUT_DEBOUNCE_MS);
  }, [captureLayoutFromApi, persistLayoutNow]);

  const filterTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const scheduleFilterSave = useCallback(() => {
    if (!gridApiRef.current || !gridInitializedRef.current) return;
    if (isFirstRenderRef.current) return;
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
    filterTimerRef.current = setTimeout(() => {
      const api = gridApiRef.current;
      if (!api || !gridInitializedRef.current) return;
      const filter = api.getFilterModel() as Record<string, unknown>;
      const sanitized = sanitizeFilterModel(filter);
      void putUserPreferences({
        [filterPrefKey(gridId)]: JSON.stringify(sanitized ?? {}),
      });
    }, FILTER_DEBOUNCE_MS);
  }, [gridId]);

  const syncColumnVisibilityState = useCallback((api: GridApi<T>) => {
    const seeded = seedColumnMetaFromDefs();
    const visibility: Record<string, boolean> = { ...seeded.visibility };
    const labels: Record<string, string> = { ...seeded.labels };

    api.getAllGridColumns()?.forEach((col) => {
      const colId = col.getColId();
      visibility[colId] = col.isVisible();
      labels[colId] = columnLabel(col);
    });

    setColumnVisibility(visibility);
    setAllColumnLabels(labels);
  }, [seedColumnMetaFromDefs]);

  const applySavedLayout = useCallback(async (api: GridApi<T>) => {
    try {
      const state = await loadGridLayout(gridId);
      if (!state || state.length === 0) return;
      lastLayoutRef.current = state;
      api.applyColumnState({ state, applyOrder: true });
    } catch {
      // Повреждённые настройки — игнорируем.
    }
  }, [gridId]);

  const applySavedFilter = useCallback(async (api: GridApi<T>, dataRowCount: number) => {
    if (dataRowCount === 0) return;
    try {
      const prefs = await getUserPreferences();
      const filterRaw = prefs[filterPrefKey(gridId)];
      if (!filterRaw) return;
      const model = JSON.parse(filterRaw) as Record<string, unknown>;
      const sanitized = sanitizeFilterModel(model);
      if (!sanitized) {
        if (Object.keys(model).length > 0) {
          await putUserPreferences({ [filterPrefKey(gridId)]: '{}' });
        }
        return;
      }
      api.setFilterModel(sanitized);
      if (api.getDisplayedRowCount() === 0) {
        api.setFilterModel(null);
        await putUserPreferences({ [filterPrefKey(gridId)]: '{}' });
      }
    } catch {
      // Повреждённые настройки — игнорируем.
    }
  }, [gridId]);

  const onGridReady = useCallback(
    (event: GridReadyEvent<T>) => {
      gridApiRef.current = event.api;
      // E2E: ensureColumnVisible через window.__epGridApis[gridId]
      if (typeof window !== 'undefined' && gridId) {
        const w = window as Window & { __epGridApis?: Record<string, GridApi> };
        w.__epGridApis = w.__epGridApis ?? {};
        w.__epGridApis[gridId] = event.api as GridApi;
      }
      onGridApiReady?.(event.api);
      void applySavedLayout(event.api).then(() => {
        syncColumnVisibilityState(event.api);
        gridInitializedRef.current = true;
        setTimeout(() => {
          isFirstRenderRef.current = false;
        }, 500);
      });
    },
    [applySavedLayout, gridId, onGridApiReady, syncColumnVisibilityState],
  );

  useEffect(() => () => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current);
      saveTimerRef.current = null;
    }
    if (filterTimerRef.current) {
      clearTimeout(filterTimerRef.current);
      filterTimerRef.current = null;
    }
    if (lastLayoutRef.current && lastLayoutRef.current.length > 0) {
      void persistGridLayout(gridId, lastLayoutRef.current, { legacyUserSetting: false });
    }
  }, [gridId]);

  useEffect(() => {
    gridInitializedRef.current = false;
    isFirstRenderRef.current = true;
    filterAppliedRef.current = false;
  }, [gridId]);

  useEffect(() => {
    const api = gridApiRef.current;
    if (!api || !gridInitializedRef.current || filterAppliedRef.current || rowData.length === 0) {
      return undefined;
    }

    const timer = window.setTimeout(() => {
      if (filterAppliedRef.current) return;
      filterAppliedRef.current = true;
      void applySavedFilter(api, rowData.length);
    }, 800);

    return () => window.clearTimeout(timer);
  }, [applySavedFilter, columnDefs, gridId, rowData.length]);

  useEffect(() => {
    const seeded = seedColumnMetaFromDefs();
    setAllColumnLabels((prev) => {
      const next = { ...prev };
      for (const [colId, label] of Object.entries(seeded.labels)) {
        if (!(colId in next)) next[colId] = label;
      }
      return next;
    });
  }, [seedColumnMetaFromDefs]);

  useEffect(() => {
    const api = gridApiRef.current;
    if (!api || !gridInitializedRef.current) return;
    void applySavedLayout(api).then(() => syncColumnVisibilityState(api));
  }, [applySavedLayout, columnDefs, syncColumnVisibilityState]);

  const getContextMenuItems = useCallback(
    (params: GetContextMenuItemsParams<T>): (string | MenuItemDef)[] => {
      if (!canRead) {
        return ['copy', 'copyWithHeaders'];
      }

      const row = params.node?.data;
      const items: (string | MenuItemDef)[] = [];

      if (treeData) {
        items.push({
          name: 'Развернуть всё',
          action: () => gridApiRef.current?.expandAll(),
        });
        items.push({
          name: 'Свернуть всё',
          action: () => gridApiRef.current?.collapseAll(),
        });
        items.push('separator');
      }

      if (canEdit && onEdit) {
        items.push({
          name: 'Редактировать',
          disabled: !row,
          action: () => row && onEdit(row),
        });
      }

      if (canEdit && onArchive) {
        items.push({
          name: 'В архив',
          disabled: !row,
          action: () => row && onArchive(row),
        });
      }

      if (canEdit && onDelete) {
        items.push({
          name: 'Удалить',
          disabled: !row,
          action: () => row && onDelete(row),
        });
      }

      const extraItems = additionalContextMenuItems
        .filter((item) => !item.requiredLevel || hasLevel(item.requiredLevel))
        .map((item) => ({
          name: item.name,
          disabled: typeof item.disabled === 'function' ? item.disabled(row) : item.disabled,
          icon: item.icon,
          action: () => item.action(row),
        }));

      return [
        ...items,
        ...extraItems,
        'separator',
        'copy',
        ...(contextMenuMode === 'full' ? (['copyWithHeaders', 'export'] as const) : []),
      ];
    },
    [additionalContextMenuItems, canEdit, canRead, contextMenuMode, hasLevel, onArchive, onDelete, onEdit, treeData],
  );

  const openColumnConfig = () => {
    const api = gridApiRef.current;
    if (api) {
      syncColumnVisibilityState(api);
    } else {
      const seeded = seedColumnMetaFromDefs();
      setAllColumnLabels(seeded.labels);
      setColumnVisibility(seeded.visibility);
    }
    setColumnConfigOpen(true);
  };

  const applyColumnVisibility = () => {
    const api = gridApiRef.current;
    if (!api) return;

    const entries = Object.entries(columnVisibility);

    entries.forEach(([colId, visible]) => {
      api.setColumnsVisible([colId], visible !== false);
    });

    const state = api.getColumnState().map((col) => ({
      ...col,
      hide: columnVisibility[col.colId] === false,
    }));
    api.applyColumnState({ state, applyOrder: true });
    syncColumnVisibilityState(api);
    setColumnConfigOpen(false);
    void persistLayoutNow();
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
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <Stack direction="row" spacing={0.5} sx={{ mb: 1, justifyContent: 'space-between', flexShrink: 0 }}>
        {treeData ? (
          <Stack direction="row" spacing={0.5}>
            <ToolbarActionButton
              onClick={() => gridApiRef.current?.expandAll()}
              aria-label="Развернуть всё"
            >
              Развернуть всё
            </ToolbarActionButton>
            <ToolbarActionButton
              onClick={() => gridApiRef.current?.collapseAll()}
              aria-label="Свернуть всё"
            >
              Свернуть всё
            </ToolbarActionButton>
          </Stack>
        ) : (
          <Box />
        )}
        <Stack direction="row" spacing={0.5}>
        <Tooltip title="Настройка столбцов">
          <IconButton size="small" onClick={openColumnConfig} aria-label="Настройка столбцов">
            <ViewColumnOutlinedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        {showExportButton && onExportExcel && (
          <ExportActionButton kind="excel" onClick={onExportExcel} />
        )}
        </Stack>
      </Stack>

      <GridViewport loading={loading}>
        <AgGridReact<T>
          containerStyle={{ height: '100%', width: '100%' }}
          ref={gridRef}
          rowData={resolvedRowData}
          columnDefs={columnDefs}
          defaultColDef={defaultColDef}
          localeText={agGridLocaleRu}
          domLayout="normal"
          animateRows={false}
          rowHeight={40}
          rowSelection="single"
          getRowId={(params) => String(params.data.id)}
          pagination={!treeData}
          paginationPageSize={100}
          cacheBlockSize={treeData ? 50 : 100}
          rowBuffer={10}
          treeData={treeData}
          getDataPath={treeData ? resolvedGetDataPath : undefined}
          autoGroupColumnDef={autoGroupColumnDef}
          groupDefaultExpanded={treeData ? -1 : undefined}
          suppressColumnVirtualisation={false}
          suppressRowVirtualisation={false}
          components={{ ExcelSetColumnFilter }}
          onGridReady={onGridReady}
          onColumnMoved={scheduleLayoutSave}
          onColumnResized={(event) => {
            if (event.finished) scheduleLayoutSave();
          }}
          onColumnVisible={scheduleLayoutSave}
          onSortChanged={scheduleLayoutSave}
          onFilterChanged={(_event: FilterChangedEvent<T>) => scheduleFilterSave()}
          getContextMenuItems={getContextMenuItems}
          onRowClicked={(event) => {
            if (event.data && onRowClicked) onRowClicked(event.data);
          }}
          onRowDoubleClicked={(event) => {
            if (event.data && onRowDoubleClicked) onRowDoubleClicked(event.data);
          }}
          overlayLoadingTemplate={GRID_OVERLAY_LOADING_TEMPLATE}
          overlayNoRowsTemplate={GRID_OVERLAY_NO_ROWS_TEMPLATE}
        />
      </GridViewport>

      <Dialog open={columnConfigOpen} onClose={() => setColumnConfigOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ color: wpColors.heading }}>Настройка столбцов</DialogTitle>
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

export const DictionaryGrid = memo(DictionaryGridInternal) as <T extends { id: number }>(
  props: DictionaryGridProps<T>
) => JSX.Element;
