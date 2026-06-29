import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Alert, Box, IconButton, MenuItem, Select, Typography } from '@mui/material';
import { AgGridReact } from 'ag-grid-react';
import type {
  ColDef,
  FilterChangedEvent,
  GridApi,
  GridReadyEvent,
  ICellRendererParams,
} from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import {
  updatePermissionMatrix,
  type MatrixAccessLevel,
} from '../../api/permissionsMatrixApi';
import { putUserPreference, getUserPreferences } from '../../api/userPreferencesApi';
import {
  buildPermissionMatrixRows,
  INDENT_PX_PER_LEVEL,
  isMatrixRowVisible,
  type PermissionMatrixRowModel,
} from '../../constants/permissionObjectHierarchy';
import { useAuthStore } from '../../store/useAuthStore';
import { usePermissionMatrixStore } from '../../store/usePermissionMatrixStore';
import { agGridLocaleRu } from '../../utils/agGridLocaleRu';

const EDITABLE_LEVELS: { value: MatrixAccessLevel; label: string }[] = [
  { value: 'None', label: 'Нет доступа' },
  { value: 'Read', label: 'Чтение' },
  { value: 'Edit', label: 'Редактирование' },
];

const ROLE_HEADER_LABELS: Record<string, string> = {
  Administrator: 'Администратор',
  ProjectManager: 'Менеджер проекта',
  Observer: 'Наблюдатель',
};

const OBJECT_COLUMN_HEADER = 'Объект / Роль';
const ADMIN_LOCKED_LABEL = 'Редактирование';

const GRID_ID = 'settings-access-rights';
const FILTER_PREF_KEY = `grid.${GRID_ID}.filter`;
const FILTER_DEBOUNCE_MS = 400;

function formatRoleHeaderName(roleName: string): string {
  return ROLE_HEADER_LABELS[roleName] ?? roleName;
}

function isAdministratorRole(roleName: string): boolean {
  return roleName === 'Administrator';
}

export function PermissionMatrixGrid() {
  const user = useAuthStore((s) => s.user);
  const isAdmin = user?.roleName === 'Administrator';

  const matrix = usePermissionMatrixStore((s) => s.matrix);
  const storeLoading = usePermissionMatrixStore((s) => s.loading);
  const storeError = usePermissionMatrixStore((s) => s.error);
  const fetchMatrix = usePermissionMatrixStore((s) => s.fetchMatrix);

  const gridApiRef = useRef<GridApi<PermissionMatrixRowModel> | null>(null);
  const filterTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [rows, setRows] = useState<PermissionMatrixRowModel[]>([]);
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!user || !isAdmin) return;
    void fetchMatrix(user.id, true);
  }, [fetchMatrix, isAdmin, user]);

  useEffect(() => {
    if (!matrix) {
      setRows([]);
      return;
    }
    setRows(buildPermissionMatrixRows(matrix));
  }, [matrix]);

  useEffect(() => () => {
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
  }, []);

  const visibleRows = useMemo(
    () => rows.filter((row) => isMatrixRowVisible(row, collapsedSections)),
    [collapsedSections, rows],
  );

  const toggleSection = useCallback((sectionId: string) => {
    setCollapsedSections((prev) => {
      const next = new Set(prev);
      if (next.has(sectionId)) next.delete(sectionId);
      else next.add(sectionId);
      return next;
    });
  }, []);

  const handleLevelChange = useCallback(async (
    objectName: string,
    roleId: number,
    roleName: string,
    level: MatrixAccessLevel,
  ) => {
    if (!matrix || isAdministratorRole(roleName)) return;

    setRows((prev) =>
      prev.map((row) =>
        row.objectName === objectName && !row.isHeader
          ? { ...row, levels: { ...row.levels, [String(roleId)]: level } }
          : row,
      ),
    );

    setSaving(true);
    setError(null);
    try {
      await updatePermissionMatrix({
        [objectName]: { [String(roleId)]: level },
      });
      usePermissionMatrixStore.setState((state) => {
        if (!state.matrix) return state;
        return {
          matrix: {
            ...state.matrix,
            matrix: {
              ...state.matrix.matrix,
              [objectName]: {
                ...(state.matrix.matrix[objectName] ?? {}),
                [String(roleId)]: level,
              },
            },
          },
        };
      });
      await useAuthStore.getState().refreshUser();
    } catch {
      setError('Не удалось обновить право.');
      if (user) await fetchMatrix(user.id, true);
    } finally {
      setSaving(false);
    }
  }, [fetchMatrix, matrix, user]);

  const scheduleFilterSave = useCallback(() => {
    if (!gridApiRef.current) return;
    if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
    filterTimerRef.current = setTimeout(() => {
      const model = gridApiRef.current?.getFilterModel();
      if (model) {
        void putUserPreference(FILTER_PREF_KEY, JSON.stringify(model));
      }
    }, FILTER_DEBOUNCE_MS);
  }, []);

  const onGridReady = useCallback(async (event: GridReadyEvent<PermissionMatrixRowModel>) => {
    gridApiRef.current = event.api;
    try {
      const prefs = await getUserPreferences();
      const raw = prefs[FILTER_PREF_KEY];
      if (raw) {
        const model = JSON.parse(raw) as Record<string, unknown>;
        event.api.setFilterModel(model);
      }
    } catch {
      // Повреждённое состояние фильтра — игнорируем.
    }
  }, []);

  const onFilterChanged = useCallback(
    (_event: FilterChangedEvent<PermissionMatrixRowModel>) => {
      scheduleFilterSave();
    },
    [scheduleFilterSave],
  );

  const renderObjectCell = useCallback(
    (params: ICellRendererParams<PermissionMatrixRowModel>) => {
      const row = params.data;
      if (!row) return null;

      const indentPx = row.level * INDENT_PX_PER_LEVEL;
      const isCollapsed = collapsedSections.has(row.objectName);

      return (
        <Box
          data-testid={`object-cell-${row.objectName}`}
          data-indent-level={row.level}
          sx={{
            display: 'flex',
            alignItems: 'center',
            pl: `${indentPx}px`,
            fontWeight: row.isHeader ? 600 : 400,
          }}
        >
          {row.isHeader && (
            <IconButton
              size="small"
              aria-label={isCollapsed ? 'Развернуть раздел' : 'Свернуть раздел'}
              data-testid={`toggle-section-${row.objectName}`}
              onClick={() => toggleSection(row.objectName)}
              sx={{ mr: 0.5 }}
            >
              {isCollapsed ? (
                <ChevronRightIcon fontSize="small" />
              ) : (
                <ExpandMoreIcon fontSize="small" />
              )}
            </IconButton>
          )}
          <Typography variant="body2">{row.displayName}</Typography>
        </Box>
      );
    },
    [collapsedSections, toggleSection],
  );

  const renderRoleCell = useCallback(
    (roleId: number, roleName: string) => (params: ICellRendererParams<PermissionMatrixRowModel>) => {
      const row = params.data;
      if (!row || row.isHeader) return null;

      if (isAdministratorRole(roleName)) {
        return (
          <Typography
            variant="body2"
            data-testid={`admin-locked-${row.objectName}-${roleId}`}
            sx={{ py: 1, color: 'text.secondary' }}
          >
            {ADMIN_LOCKED_LABEL}
          </Typography>
        );
      }

      const value: MatrixAccessLevel = row.levels[String(roleId)] ?? 'None';

      return (
        <Select
          size="small"
          value={value}
          onChange={(e) =>
            void handleLevelChange(row.objectName, roleId, roleName, e.target.value as MatrixAccessLevel)
          }
          disabled={saving}
          data-testid={`role-select-${row.objectName}-${roleId}`}
          sx={{
            width: '100%',
            fontSize: 13,
            color: value === 'None' ? 'text.secondary' : 'text.primary',
          }}
        >
          {EDITABLE_LEVELS.map((item) => (
            <MenuItem key={item.value} value={item.value}>
              {item.label}
            </MenuItem>
          ))}
        </Select>
      );
    },
    [handleLevelChange, saving],
  );

  const columnDefs = useMemo<ColDef<PermissionMatrixRowModel>[]>(() => {
    if (!matrix) {
      return [
        {
          colId: 'objectName',
          headerName: OBJECT_COLUMN_HEADER,
          pinned: 'left',
          minWidth: 320,
        },
      ];
    }

    const objectColumn: ColDef<PermissionMatrixRowModel> = {
      colId: 'objectName',
      field: 'displayName',
      headerName: OBJECT_COLUMN_HEADER,
      pinned: 'left',
      lockPinned: true,
      minWidth: 320,
      flex: 0,
      suppressMovable: true,
      sortable: false,
      filter: false,
      cellRenderer: renderObjectCell,
    };

    const roleColumns: ColDef<PermissionMatrixRowModel>[] = matrix.roles.map((role) => ({
      colId: `role_${role.id}`,
      field: `levels.${role.id}`,
      headerName: formatRoleHeaderName(role.name),
      minWidth: 168,
      flex: 1,
      filter: false,
      sortable: false,
      suppressMovable: true,
      editable: false,
      cellRenderer: renderRoleCell(role.id, role.name),
    }));

    return [objectColumn, ...roleColumns];
  }, [matrix, renderObjectCell, renderRoleCell]);

  const combinedError = error ?? storeError;

  if (!isAdmin) {
    return <Alert severity="warning">Матрица прав доступна только администратору.</Alert>;
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        flex: 1,
        minHeight: 0,
        height: '100%',
        overflow: 'hidden',
      }}
    >
      <Typography variant="h5" sx={{ mb: 2, flexShrink: 0 }}>
        Матрица прав доступа
      </Typography>
      {combinedError && (
        <Alert severity="error" sx={{ mb: 2, flexShrink: 0 }}>
          {combinedError}
        </Alert>
      )}
      <Box
        className="ag-theme-alpine"
        data-testid="permission-matrix-grid"
        sx={{
          flex: 1,
          minHeight: 0,
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
          opacity: storeLoading ? 0.6 : 1,
          '& .ag-root-wrapper': { flex: 1, minHeight: 0, height: '100%' },
          '& .ag-root-wrapper-body': { height: '100%' },
          '& .ag-root': { height: '100%' },
        }}
      >
        <AgGridReact<PermissionMatrixRowModel>
          rowData={visibleRows}
          columnDefs={columnDefs}
          domLayout="normal"
          headerHeight={42}
          localeText={agGridLocaleRu}
          defaultColDef={{
            sortable: false,
            filter: false,
            resizable: true,
          }}
          animateRows
          rowHeight={44}
          getRowId={(params) => params.data.id}
          onGridReady={onGridReady}
          onFilterChanged={onFilterChanged}
          overlayLoadingTemplate='<span class="ag-overlay-loading-center">Загрузка...</span>'
          overlayNoRowsTemplate='<span class="ag-overlay-loading-center">Нет данных</span>'
        />
      </Box>
    </Box>
  );
}
