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
import { usePermission } from '../../hooks/usePermission';
import { wpColors } from '../../theme/wpColors';
import { agGridLocaleRu } from '../../utils/agGridLocaleRu';
import { ExcelSetColumnFilter } from './ExcelSetColumnFilter';

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
  objectName: string;
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
  additionalContextMenuItems?: DictionaryContextMenuItem<T>[];
}

const LAYOUT_DEBOUNCE_MS = 1000;
const FILTER_DEBOUNCE_MS = 800;

const SET_FILTER_PARAMS = {
  buttons: ['apply', 'reset'] as const,
  closeOnApply: true,
  suppressSorting: true,
  excelMode: 'windows' as const,
};

function layoutPrefKey(gridId: string): string {
  return `grid.${gridId}.layout`;
}

function filterPrefKey(gridId: string): string {
  return `grid.${gridId}.filter`;
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
  additionalContextMenuItems = [],
}: DictionaryGridProps<T>) {
  console.log(`[DictionaryGrid ${gridId}] Render: rowData.length=${rowData.length}, loading=${loading}, treeData=${treeData}`);

  const gridRef = useRef<AgGridReact<T>>(null);
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const gridInitializedRef = useRef(false);
  const isFirstRenderRef = useRef(true);

  const { canRead, canEdit: canEditPermission, hasLevel } = usePermission(objectName);
  const canEdit = canEditProp ?? canEditPermission;

  const [columnConfigOpen, setColumnConfigOpen] = useState(false);
  const [columnVisibility, setColumnVisibility] = useState<Record<string, boolean>>({});
  const [allColumnLabels, setAllColumnLabels] = useState<Record<string, string>>({});

  const seedColumnMetaFromDefs = useCallback(() => {
    const labels: Record<string, string> = {};
    const visibility: Record<string, boolean> = {};

    columnDefs.forEach((col, index) => {
      const colId = col.colId ?? col.field ?? `col_${index}`;
      labels[colId] = col.headerName ?? colId;
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
      flex: 1,
      minWidth: 120,
    }),
    [],
  );

  const scheduleLayoutSave = useCallback(() => {
    if (!gridApiRef.current || !gridInitializedRef.current) return;
    // Skip saving during initial render to avoid unnecessary API calls
    if (isFirstRenderRef.current) return;
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    saveTimerRef.current = setTimeout(() => {
      const api = gridApiRef.current;
      if (!api || !gridInitializedRef.current) return;
      const layout = api.getColumnState();
      void putUserPreferences({
        [layoutPrefKey(gridId)]: JSON.stringify(layout),
      });
    }, LAYOUT_DEBOUNCE_MS);
  }, [gridId]);

  const filterTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const scheduleFilterSave = useCallback(() => {
    if (!gridApiRef.current || !gridInitializedRef.current) return;
    if (isFirstRenderRef.current) return;
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
    filterTimerRef.current = setTimeout(() => {
      const api = gridApiRef.current;
      if (!api || !gridInitializedRef.current) return;
      const filter = api.getFilterModel();
      void putUserPreferences({
        [filterPrefKey(gridId)]: JSON.stringify(filter),
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

  const applySavedPreferences = useCallback(async (api: GridApi<T>) => {
    try {
      const prefs = await getUserPreferences();
      const layoutRaw = prefs[layoutPrefKey(gridId)];
      if (layoutRaw) {
        const state = JSON.parse(layoutRaw) as ColumnState[];
        api.applyColumnState({ state, applyOrder: true });
      }
      // НЕ применяем сохранённые фильтры автоматически - они скрывают новые данные
      // const filterRaw = prefs[filterPrefKey(gridId)];
      // if (filterRaw) {
      //   const model = JSON.parse(filterRaw) as Record<string, unknown>;
      //   api.setFilterModel(model);
      // }
    } catch {
      // Повреждённые настройки — игнорируем.
    }
  }, [gridId]);

  const onGridReady = useCallback(
    (event: GridReadyEvent<T>) => {
      console.log(`[DictionaryGrid ${gridId}] onGridReady called`);
      gridApiRef.current = event.api;
      console.log(`[DictionaryGrid ${gridId}] API set, rowData.length=${rowData.length}`);
      void applySavedPreferences(event.api).then(() => {
        console.log(`[DictionaryGrid ${gridId}] Preferences applied`);
        syncColumnVisibilityState(event.api);
        // Mark grid as initialized after preferences are applied
        gridInitializedRef.current = true;
        console.log(`[DictionaryGrid ${gridId}] Grid initialized, rowCount=${event.api.getDisplayedRowCount()}`);
        // Clear first render flag after a short delay to skip initial events
        setTimeout(() => {
          isFirstRenderRef.current = false;
        }, 500);
      });
    },
    [applySavedPreferences, gridId, rowData.length, syncColumnVisibilityState],
  );

  useEffect(() => () => {
    if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
  }, []);

  // Reset initialization flags and filters when rowData changes (new data loaded)
  useEffect(() => {
    console.log(`[DictionaryGrid ${gridId}] rowData changed: length=${rowData.length}`);
    const api = gridApiRef.current;
    if (api) {
      console.log(`[DictionaryGrid ${gridId}] Current grid row count: ${api.getDisplayedRowCount()}`);
      // Сбрасываем фильтры при загрузке новых данных - иначе старые фильтры скрывают новые данные
      if (rowData.length > 0) {
        const currentFilter = api.getFilterModel();
        if (Object.keys(currentFilter).length > 0) {
          console.log(`[DictionaryGrid ${gridId}] Resetting filters to show new data`);
          api.setFilterModel({});
        }
      }
    }
    if (rowData && rowData.length > 0) {
      console.log(`[DictionaryGrid ${gridId}] Data loaded, first row:`, rowData[0]);
      // Data loaded, but wait before allowing saves to skip grid init events
      const timer = setTimeout(() => {
        isFirstRenderRef.current = false;
        if (gridApiRef.current) {
          console.log(`[DictionaryGrid ${gridId}] After timeout - grid row count: ${gridApiRef.current.getDisplayedRowCount()}`);
        }
      }, 600);
      return () => clearTimeout(timer);
    }
  }, [gridId, rowData]);

  useEffect(() => {
    const seeded = seedColumnMetaFromDefs();
    setAllColumnLabels((prev) => ({ ...seeded.labels, ...prev }));
    setColumnVisibility((prev) => ({ ...seeded.visibility, ...prev }));
  }, [seedColumnMetaFromDefs]);

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
        'copyWithHeaders',
        'export',
      ];
    },
    [additionalContextMenuItems, canEdit, canRead, hasLevel, onArchive, onDelete, onEdit, treeData],
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
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0, overflow: 'hidden' }}>
      <Stack direction="row" spacing={0.5} sx={{ mb: 1, justifyContent: 'space-between', flexShrink: 0 }}>
        {treeData ? (
          <Stack direction="row" spacing={0.5}>
            <Button
              size="small"
              variant="outlined"
              onClick={() => gridApiRef.current?.expandAll()}
              sx={{ textTransform: 'none' }}
              aria-label="Развернуть всё"
            >
              Развернуть всё
            </Button>
            <Button
              size="small"
              variant="outlined"
              onClick={() => gridApiRef.current?.collapseAll()}
              sx={{ textTransform: 'none' }}
              aria-label="Свернуть всё"
            >
              Свернуть всё
            </Button>
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
          <Button
            size="small"
            variant="outlined"
            onClick={onExportExcel}
            sx={{ textTransform: 'none' }}
          >
            Выгрузить в Excel
          </Button>
        )}
        </Stack>
      </Stack>

      <Box
        className="ag-theme-alpine"
        sx={{
          flex: 1,
          minHeight: 0,
          width: '100%',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
          opacity: loading ? 0.6 : 1,
        }}
      >
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            width: '100%',
            height: '100%',
            overflow: 'hidden',
            '& .ag-root-wrapper': { height: '100%' },
            '& .ag-root-wrapper-body': { height: '100%' },
            '& .ag-root': { height: '100%' },
          }}
        >
        <AgGridReact<T>
          containerStyle={{ height: '100%', width: '100%' }}
          ref={gridRef}
          rowData={rowData}
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
          onColumnResized={scheduleLayoutSave}
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
          overlayLoadingTemplate='<span class="ag-overlay-loading-center">Загрузка...</span>'
          overlayNoRowsTemplate='<span class="ag-overlay-loading-center">Нет данных</span>'
        />
        </Box>
      </Box>

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
