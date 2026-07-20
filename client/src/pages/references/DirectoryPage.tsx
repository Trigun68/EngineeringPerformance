import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Box, TextField } from '@mui/material';
import type { ColDef, GridApi, ValueGetterParams } from 'ag-grid-community';
import {
  archiveDirectoryEntry,
  createDirectoryEntry,
  deleteDirectoryEntry,
  exportDirectoryExcel,
  fetchCharacteristicOptions,
  fetchDirectory,
  fetchDirectoryTree,
  flattenDirectoryTree,
  unarchiveDirectoryEntry,
  updateDirectoryEntry,
  type DirectoryEntry,
  type DirectoryTreeRow,
} from '../../api/directoriesApi';
import { getApiErrorMessage } from '../../services/apiError';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import { HierarchicalSelect } from '../../components/dictionary/HierarchicalSelect';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { usePermission } from '../../hooks/usePermission';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { CharacteristicEditDialog } from './CharacteristicEditDialog';
import { DirectoryEditDialog, DirectoryFormRow } from './DirectoryEditDialog';
import { DirectoryFormFields } from './DirectoryFormFields';
import { DirectoryToolbar } from './DirectoryToolbar';
import {
  buildPropertiesPayload,
  emptyProperties,
  formatPropertyValue,
  getDirectoryFields,
  getFormFields,
  propertiesFromEntry,
} from './directoryFieldConfig';
import { validateDirectoryWrite } from './directoryWriteSchema';
import { z } from 'zod';
import type { DirectoryFieldDef } from './directoryFieldConfig';
import type { ReferenceRouteConfig } from './referenceConfig';
import { isPhysicalVolumeCatalogResource } from './referenceConfig';
import { formatAsIisNorm } from '../../utils/formatIisNorm';
import { resolveIicDirectionUnitName } from '../../utils/resolveIicDirectionUnit';
import { collectGridExportData } from '../../utils/directoryExport';

interface DirectoryPageProps {
  config: ReferenceRouteConfig;
}

function boolArchive(value: boolean): string {
  return value ? 'Да' : 'Нет';
}

function propertyValue<T extends DirectoryEntry>(row: T, field: DirectoryFieldDef): unknown {
  const key = field.propertyKey ?? field.key;
  return row.properties[key] ?? row.properties[field.key];
}

export function DirectoryPage({ config }: DirectoryPageProps) {
  const { canRead, canEdit } = usePermission(config.permission);
  const invalidateDictionaries = useDictionaryStore((s) => s.invalidate);
  const fetchAllDictionaries = useDictionaryStore((s) => s.fetchAll);
  const getName = useDictionaryStore((s) => s.getName);
  const getById = useDictionaryStore((s) => s.getById);
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const directions = useDictionaryStore((s) => s.lists.iicWorkDirections);

  const resolveIicUnitName = useCallback(
    (directionId: number) => resolveIicDirectionUnitName(directionId, directions, getUnitName),
    [directions, getUnitName],
  );

  const isPhysicalVolumeCatalog = isPhysicalVolumeCatalogResource(config.resource);

  const fieldDefs = useMemo(() => getDirectoryFields(config.resource), [config.resource]);
  const formFields = useMemo(() => getFormFields(config.resource), [config.resource]);

  const [rows, setRows] = useState<DirectoryEntry[]>([]);
  const [treeRows, setTreeRows] = useState<DirectoryTreeRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showArchived, setShowArchived] = useState(false);
  const showArchivedPrefKey = `references.${config.resource}.showArchived`;

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences().then((prefs) => {
      if (!cancelled && prefs[showArchivedPrefKey] === 'true') {
        setShowArchived(true);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [showArchivedPrefKey]);

  const handleShowArchivedChange = useCallback((value: boolean) => {
    setShowArchived(value);
    void putUserPreferences({ [showArchivedPrefKey]: value ? 'true' : 'false' });
  }, [showArchivedPrefKey]);
  const [selected, setSelected] = useState<DirectoryEntry | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<DirectoryEntry | null>(null);
  const [name, setName] = useState('');
  const [parentId, setParentId] = useState<number | ''>('');
  const [properties, setProperties] = useState<Record<string, unknown>>({});
  const [formIsArchived, setFormIsArchived] = useState(false);
  const [characteristicModalOpen, setCharacteristicModalOpen] = useState(false);
  const [characteristicEditing, setCharacteristicEditing] = useState<DirectoryEntry | null>(null);

  const [optionCounts, setOptionCounts] = useState<Record<number, number>>({});

  const gridApiRef = useRef<GridApi | null>(null);

  useEffect(() => {
    void fetchAllDictionaries();
  }, [fetchAllDictionaries]);

  // Сброс выбора при смене вкладки справочника.
  useEffect(() => {
    setSelected(null);
    void fetchAllDictionaries();
  }, [config.resource, fetchAllDictionaries]);

  const directoryQuery = useQuery({
    queryKey: ['directory-page', config.resource, showArchived],
    enabled: canRead,
    staleTime: 30_000,
    queryFn: async () => {
      if (config.isHierarchical) {
        const tree = await fetchDirectoryTree(config.resource, showArchived);
        const flattened = flattenDirectoryTree(tree, [], {
          hierarchyByCode: isPhysicalVolumeCatalogResource(config.resource),
        });
        return { kind: 'tree' as const, treeRows: flattened, rows: [] as DirectoryEntry[], optionCounts: {} as Record<number, number> };
      }

      const paged = await fetchDirectory(config.resource, showArchived, 1, 100);
      const counts: Record<number, number> = {};
      if (config.resource === 'project-characteristics') {
        for (const item of paged.items) {
          try {
            const opts = await fetchCharacteristicOptions(item.id);
            counts[item.id] = opts.length;
          } catch {
            counts[item.id] = 0;
          }
        }
      }
      return { kind: 'flat' as const, treeRows: [] as DirectoryTreeRow[], rows: paged.items, optionCounts: counts };
    },
  });

  const load = useCallback(async () => {
    await directoryQuery.refetch();
  }, [directoryQuery]);

  useEffect(() => {
    if (!directoryQuery.data) return;
    if (directoryQuery.data.kind === 'tree') {
      setTreeRows(directoryQuery.data.treeRows);
    } else {
      setRows(directoryQuery.data.rows);
      setOptionCounts(directoryQuery.data.optionCounts);
    }
    setSelected(null);
  }, [directoryQuery.data]);

  useEffect(() => {
    setLoading(directoryQuery.isFetching);
    if (directoryQuery.error) {
      const err = directoryQuery.error;
      const status = (err as { response?: { status?: number } })?.response?.status;
      const hint =
        status === 401
          ? ' Сессия истекла — войдите снова.'
          : status === 403
            ? ' Недостаточно прав.'
            : status === 410
              ? ' Запрос ушёл не в Engineering Performance (проверьте Cloudflare Tunnel / DNS web на edge).'
              : '';
      setError(`${getApiErrorMessage(err, 'Не удалось загрузить справочник.')}${hint}`);
    } else if (!directoryQuery.isFetching) {
      setError(null);
    }
  }, [directoryQuery.error, directoryQuery.isFetching]);

  const buildPropertyColumns = useCallback(
    <T extends DirectoryEntry>(): ColDef<T>[] =>
      fieldDefs
        .filter((f) => f.showInGrid)
        .map((field) => ({
          colId: field.key,
          headerName: field.label,
          minWidth: field.gridWidth ?? 120,
          valueGetter: (params: ValueGetterParams<T>) => {
            const row = params.data;
            if (!row) return '';
            const value = propertyValue(row, field);
            return formatPropertyValue(field, value, getName, getById, getUnitName, resolveIicUnitName);
          },
        })),
    [fieldDefs, getById, getName, getUnitName, resolveIicUnitName],
  );

  const archiveColDef = useMemo<ColDef>(
    () => ({
      colId: 'isArchived',
      field: 'isArchived',
      headerName: 'Архив',
      maxWidth: 80,
      valueFormatter: (p) => boolArchive(Boolean(p.value)),
    }),
    [],
  );

  const propertyColumns = useMemo(() => buildPropertyColumns<DirectoryEntry>(), [buildPropertyColumns]);
  const treePropertyColumns = useMemo(() => buildPropertyColumns<DirectoryTreeRow>(), [buildPropertyColumns]);

  const flatColumnDefs = useMemo<ColDef<DirectoryEntry>[]>(() => {
    if (config.resource === 'project-characteristics') {
      return [
        { colId: 'name', field: 'name', headerName: 'Наименование характеристики', minWidth: 280, flex: 1 },
        {
          colId: 'optionCount',
          headerName: 'Кол-во вариантов',
          maxWidth: 140,
          valueGetter: (p) => (p.data ? (optionCounts[p.data.id] ?? 0) : 0),
        },
        archiveColDef,
      ];
    }
    if (isPhysicalVolumeCatalog) {
      return [
        {
          colId: 'code',
          headerName: 'Код',
          minWidth: 120,
          valueGetter: (p) => {
            const code = p.data?.properties?.code;
            return code != null ? String(code) : '';
          },
        },
        { colId: 'name', field: 'name', headerName: 'Наименование', minWidth: 240, flex: 1 },
        ...propertyColumns,
        archiveColDef,
      ];
    }
    return [
      { colId: 'name', field: 'name', headerName: 'Наименование', minWidth: 240, flex: 1 },
      ...propertyColumns,
      archiveColDef,
    ];
  }, [archiveColDef, config.resource, isPhysicalVolumeCatalog, optionCounts, propertyColumns]);

  const nameColumnDef = useMemo<ColDef<DirectoryTreeRow>>(
    () => ({ colId: 'name', field: 'name', headerName: 'Наименование', minWidth: 240, flex: 1 }),
    [],
  );

  const treeColumnDefs = useMemo<ColDef<DirectoryTreeRow>[]>(
    () => (isPhysicalVolumeCatalog
      ? [nameColumnDef, ...treePropertyColumns, archiveColDef as ColDef<DirectoryTreeRow>]
      : [...treePropertyColumns, archiveColDef as ColDef<DirectoryTreeRow>]),
    [archiveColDef, isPhysicalVolumeCatalog, nameColumnDef, treePropertyColumns],
  );

  const parentOptions = useMemo(
    () => (config.isHierarchical ? treeRows : rows).filter((r) => !r.isArchived),
    [config.isHierarchical, rows, treeRows],
  );

  const handleGridApiReady = useCallback((api: GridApi) => {
    gridApiRef.current = api;
  }, []);

  const handleExport = useCallback(async () => {
    const api = gridApiRef.current;
    if (!api) return;
    const { headers, rows: exportRows } = collectGridExportData(api);
    await exportDirectoryExcel(config.resource, config.title, headers, exportRows);
  }, [config.resource, config.title]);

  const autoGroupColumnDef = useMemo<ColDef<DirectoryTreeRow>>(() => {
    if (isPhysicalVolumeCatalog) {
      return {
        colId: 'ag-Grid-AutoColumn',
        headerName: 'Код',
        minWidth: 280,
        valueGetter: (params) => {
          const row = params.data;
          if (!row) return '';
          const code = row.properties.code;
          return code != null ? String(code) : '';
        },
        cellRendererParams: { suppressCount: true },
      };
    }

    const base: ColDef<DirectoryTreeRow> = {
      colId: 'ag-Grid-AutoColumn',
      headerName: 'Наименование',
      minWidth: 280,
      field: 'name',
      cellRendererParams: { suppressCount: true },
    };

    if (config.resource === 'equipment') {
      return {
        ...base,
        valueFormatter: (params) => {
          const row = params.data;
          if (!row) return '';
          const unitId = row.properties.unitId;
          return formatAsIisNorm(
            null,
            row.name,
            getUnitName(typeof unitId === 'number' ? unitId : null),
          );
        },
      };
    }

    if (config.resource === 'iic-work-directions') {
      return {
        ...base,
        valueFormatter: (params) => {
          const row = params.data;
          if (!row) return '';
          const code = row.properties.code;
          const unitId = row.properties.unitId;
          return formatAsIisNorm(
            typeof code === 'string' ? code : null,
            row.name,
            getUnitName(typeof unitId === 'number' ? unitId : null),
          );
        },
      };
    }

    return base;
  }, [config.resource, getUnitName, isPhysicalVolumeCatalog]);

  const openCreate = () => {
    setEditing(null);
    setName('');
    setParentId('');
    setProperties(emptyProperties(config.resource));
    setFormIsArchived(false);
    setModalOpen(true);
  };

  const openEdit = (row: DirectoryEntry) => {
    setEditing(row);
    setName(row.name);
    setParentId(row.parentId ?? '');
    setProperties(propertiesFromEntry(config.resource, row.properties));
    setFormIsArchived(row.isArchived);
    setModalOpen(true);
  };

  const syncArchiveState = async (entryId: number, wasArchived: boolean) => {
    if (formIsArchived && !wasArchived) {
      await archiveDirectoryEntry(config.resource, entryId);
    } else if (!formIsArchived && wasArchived) {
      await unarchiveDirectoryEntry(config.resource, entryId);
    }
  };

  const handleSubmit = async () => {
    if (!name.trim()) return;
    const body = {
      name: name.trim(),
      parentId: parentId === '' ? null : Number(parentId),
      sortOrder: editing?.sortOrder ?? 0,
      properties: buildPropertiesPayload(config.resource, properties),
    };
    try {
      validateDirectoryWrite(body);
    } catch (err) {
      setError(err instanceof z.ZodError ? err.issues[0]?.message ?? 'Некорректные данные формы.' : 'Некорректные данные формы.');
      return;
    }
    if (editing) {
      await updateDirectoryEntry(config.resource, editing.id, body);
      await syncArchiveState(editing.id, editing.isArchived);
    } else {
      const created = await createDirectoryEntry(config.resource, body);
      if (formIsArchived) {
        await archiveDirectoryEntry(config.resource, created.id);
      }
    }

    setModalOpen(false);
    await invalidateDictionaries();
    await load();
  };

  const handleDeleteForRow = async (row: DirectoryEntry) => {
    if (!window.confirm(`Удалить «${row.name}»?`)) return;
    await deleteDirectoryEntry(config.resource, row.id);
    await invalidateDictionaries();
    await load();
  };

  const handleArchiveForRow = async (row: DirectoryEntry) => {
    if (row.isArchived) return;
    await archiveDirectoryEntry(config.resource, row.id);
    await invalidateDictionaries();
    await load();
  };

  const handleDelete = async () => {
    if (!selected || !window.confirm(`Удалить «${selected.name}»?`)) return;
    await deleteDirectoryEntry(config.resource, selected.id);
    await invalidateDictionaries();
    await load();
  };

  const handleArchive = async () => {
    if (!selected || selected.isArchived) return;
    await archiveDirectoryEntry(config.resource, selected.id);
    await invalidateDictionaries();
    await load();
  };

  const handleUnarchive = async () => {
    if (!selected || !selected.isArchived) return;
    await unarchiveDirectoryEntry(config.resource, selected.id);
    await invalidateDictionaries();
    await load();
  };

  const handleSelectRow = (row: DirectoryEntry) => {
    setSelected(row);
  };

  const handleRowDoubleClick = (row: DirectoryEntry) => {
    if (!canEdit) return;
    if (config.resource === 'project-characteristics') {
      setCharacteristicEditing(row);
      setCharacteristicModalOpen(true);
      return;
    }
    openEdit(row);
  };

  const modalTitle = editing ? `Редактирование: ${editing.name}` : 'Новая запись';
  const dialogWidth = config.resource === 'labor-intensities' ? 'md' : 'sm';
  const parentThreshold = config.parentFormOrder ?? 999;
  const codeField = formFields.find((f) => f.key === 'code');
  const propertyFormFields = formFields.filter((f) => f.key !== 'code');
  const fieldsBeforeParent = propertyFormFields.filter((f) => (f.formOrder ?? 99) < parentThreshold);
  const fieldsAfterParent = propertyFormFields.filter((f) => (f.formOrder ?? 99) >= parentThreshold);

  const parentField = config.isHierarchical ? (
    <DirectoryFormRow label="Родитель">
      <HierarchicalSelect
        items={parentOptions.map((p) => ({
          id: p.id,
          name: p.name,
          parentId: p.parentId,
          code: typeof p.properties?.code === 'string' ? p.properties.code : undefined,
        }))}
        value={parentId === '' ? null : Number(parentId)}
        onChange={(value) => setParentId(value ?? '')}
        emptyLabel="(корень)"
        excludeIds={editing ? [editing.id] : []}
        formatLabel={
          config.resource === 'equipment'
            ? (item) => {
                const row = parentOptions.find((p) => p.id === item.id);
                const unitId = row?.properties?.unitId;
                return formatAsIisNorm(
                  item.code,
                  item.name,
                  getUnitName(typeof unitId === 'number' ? unitId : null),
                );
              }
            : undefined
        }
      />
    </DirectoryFormRow>
  ) : null;

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр справочника.</Alert>;
  }

  const gridId = `ref-${config.resource}`;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <Box sx={{ flexShrink: 0 }}>
        <DirectoryToolbar
        canEdit={canEdit}
        hasSelection={selected !== null}
        showExcel={config.showExcel !== false}
        showArchived={showArchived}
        onShowArchivedChange={handleShowArchivedChange}
        onAdd={openCreate}
        onEdit={() => {
          if (!selected) return;
          if (config.resource === 'project-characteristics') {
            handleRowDoubleClick(selected);
          } else {
            openEdit(selected);
          }
        }}
        onDelete={() => void handleDelete()}
        onArchive={() => void handleArchive()}
        onUnarchive={() => void handleUnarchive()}
        onRefresh={() => void load()}
        onExport={() => void handleExport()}
        />
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2, flexShrink: 0 }}>{error}</Alert>}

      <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        {config.isHierarchical ? (
          <DictionaryGrid<DirectoryTreeRow>
            key={`${gridId}-tree`}
            rowData={treeRows}
            columnDefs={treeColumnDefs}
            loading={loading}
            objectName={config.permission}
            gridId={gridId}
            treeData
            hierarchyField="hierarchy"
            autoGroupColumnDef={autoGroupColumnDef}
            onGridApiReady={handleGridApiReady}
            canEdit={canEdit}
            onRowClicked={handleSelectRow}
            onRowDoubleClicked={canEdit ? handleRowDoubleClick : undefined}
            onEdit={canEdit ? openEdit : undefined}
            onArchive={(row) => void handleArchiveForRow(row)}
            onDelete={(row) => void handleDeleteForRow(row)}
          />
        ) : (
          <DictionaryGrid<DirectoryEntry>
            key={`${gridId}-flat`}
            rowData={rows}
            columnDefs={flatColumnDefs}
            loading={loading}
            objectName={config.permission}
            gridId={gridId}
            onGridApiReady={handleGridApiReady}
            canEdit={canEdit}
            onRowClicked={handleSelectRow}
            onRowDoubleClicked={canEdit ? handleRowDoubleClick : undefined}
            onEdit={canEdit ? openEdit : undefined}
            onArchive={(row) => void handleArchiveForRow(row)}
            onDelete={(row) => void handleDeleteForRow(row)}
          />
        )}
      </Box>

      <CharacteristicEditDialog
        open={characteristicModalOpen}
        characteristic={characteristicEditing}
        canEdit={canEdit}
        onClose={() => {
          setCharacteristicModalOpen(false);
          setCharacteristicEditing(null);
        }}
        onSaved={load}
      />

      <DirectoryEditDialog
        open={modalOpen}
        title={modalTitle}
        onClose={() => setModalOpen(false)}
        onSubmit={handleSubmit}
        maxWidth={dialogWidth}
      >
        {codeField && (
          <DirectoryFormRow label="Код">
            <TextField
              value={String(properties.code ?? '')}
              onChange={(e) => setProperties((p) => ({ ...p, code: e.target.value }))}
              fullWidth
              size="small"
              required={isPhysicalVolumeCatalog}
            />
          </DirectoryFormRow>
        )}
        <DirectoryFormRow label="Наименование">
          <TextField
            value={name}
            onChange={(e) => setName(e.target.value)}
            fullWidth
            size="small"
            required
          />
        </DirectoryFormRow>
        <DirectoryFormFields
          fields={fieldsBeforeParent}
          properties={properties}
          onPropertyChange={(key, value) => setProperties((prev) => ({ ...prev, [key]: value }))}
          isArchived={formIsArchived}
          onIsArchivedChange={setFormIsArchived}
          showArchive
        />
        {parentField}
        <DirectoryFormFields
          fields={fieldsAfterParent}
          properties={properties}
          onPropertyChange={(key, value) => setProperties((prev) => ({ ...prev, [key]: value }))}
        />
      </DirectoryEditDialog>
    </Box>
  );
}
