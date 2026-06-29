import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, Box, MenuItem, Stack, TextField } from '@mui/material';
import type { ColDef, ValueGetterParams } from 'ag-grid-community';
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
import type { DirectoryFieldDef } from './directoryFieldConfig';
import type { ReferenceRouteConfig } from './referenceConfig';
import { isPhysicalVolumeCatalogResource } from './referenceConfig';
import { formatAsIisNorm } from '../../utils/formatIisNorm';

interface DirectoryPageProps {
  config: ReferenceRouteConfig;
}

const ASSESSMENT_FILTER_OPTIONS = [
  { value: '', label: 'Все' },
  { value: 'true', label: 'Да' },
  { value: 'false', label: 'Нет' },
];

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

  const isPhysicalVolumeCatalog = isPhysicalVolumeCatalogResource(config.resource);

  const fieldDefs = useMemo(() => getDirectoryFields(config.resource), [config.resource]);
  const formFields = useMemo(() => getFormFields(config.resource), [config.resource]);

  const [rows, setRows] = useState<DirectoryEntry[]>([]);
  const [treeRows, setTreeRows] = useState<DirectoryTreeRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showArchived, setShowArchived] = useState(false);
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

  const [assessmentFilter, setAssessmentFilter] = useState('');

  useEffect(() => {
    void fetchAllDictionaries();
  }, [fetchAllDictionaries]);

  // Сброс фильтров и словарей при смене вкладки справочника.
  useEffect(() => {
    setAssessmentFilter('');
    setSelected(null);
    void fetchAllDictionaries();
  }, [config.resource, fetchAllDictionaries]);

  // AbortController для отмены запросов при смене вкладки
  const abortControllerRef = useRef<AbortController | null>(null);

  const load = useCallback(async () => {
    if (!canRead) return;

    // Отменяем предыдущий запрос
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    abortControllerRef.current = new AbortController();

    console.log(`[DirectoryPage] Loading started: resource=${config.resource}, isHierarchical=${config.isHierarchical}`);

    setLoading(true);
    setError(null);

    // Очищаем предыдущие данные перед загрузкой новых
    setRows([]);
    setTreeRows([]);

    try {
      if (config.isHierarchical) {
        console.log(`[DirectoryPage] Fetching tree for ${config.resource}...`);
        const tree = await fetchDirectoryTree(config.resource, showArchived);
        console.log(`[DirectoryPage] Tree fetched: ${tree.length} root nodes`);

        // Проверяем не был ли запрос отменён
        if (abortControllerRef.current?.signal.aborted) {
          console.log('[DirectoryPage] Request aborted, skipping setTreeRows');
          return;
        }

        const flattened = flattenDirectoryTree(tree, [], { hierarchyByCode: isPhysicalVolumeCatalogResource(config.resource) });
        console.log(`[DirectoryPage] Flattened: ${flattened.length} rows, sample:`, flattened.slice(0, 3));
        setTreeRows(flattened);
        console.log(`[DirectoryPage] setTreeRows called with ${flattened.length} rows`);
      } else {
        console.log(`[DirectoryPage] Fetching flat list for ${config.resource}...`);
        const paged = await fetchDirectory(config.resource, showArchived, 1, 100);
        console.log(`[DirectoryPage] List fetched: ${paged.items.length} items, total=${paged.totalCount}`);

        if (abortControllerRef.current?.signal.aborted) {
          console.log('[DirectoryPage] Request aborted, skipping setRows');
          return;
        }

        setRows(paged.items);
        console.log(`[DirectoryPage] setRows called with ${paged.items.length} items`);

        if (config.resource === 'project-characteristics') {
          console.log('[DirectoryPage] Loading characteristic options...');
          const counts: Record<number, number> = {};
          // Последовательно загружаем options чтобы избежать race conditions
          for (const item of paged.items) {
            if (abortControllerRef.current?.signal.aborted) return;
            try {
              const opts = await fetchCharacteristicOptions(item.id);
              counts[item.id] = opts.length;
            } catch {
              counts[item.id] = 0;
            }
          }
          setOptionCounts(counts);
          console.log('[DirectoryPage] Option counts loaded:', counts);
        }
      }
      setSelected(null);
      console.log('[DirectoryPage] Loading completed successfully');
    } catch (err) {
      if ((err as { name?: string })?.name === 'AbortError') {
        console.log('[DirectoryPage] Aborted');
        return;
      }

      console.error('[DirectoryPage] Loading error:', err);
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
    } finally {
      setLoading(false);
      console.log(`[DirectoryPage] Loading finished, loading=false`);
    }
  }, [canRead, config.resource, config.isHierarchical, showArchived]);

  useEffect(() => {
    void load();
    // Cleanup при размонтировании или смене зависимостей
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
        abortControllerRef.current = null;
      }
    };
  }, [load]);

  const buildPropertyColumns = useCallback(
    <T extends DirectoryEntry>(): ColDef<T>[] =>
      fieldDefs
        .filter((f) => f.showInGrid)
        .map((field) => ({
          headerName: field.label,
          minWidth: field.gridWidth ?? 120,
          valueGetter: (params: ValueGetterParams<T>) => {
            const row = params.data;
            if (!row) return '';
            const value = propertyValue(row, field);
            return formatPropertyValue(field, value, getName, getById, getUnitName);
          },
        })),
    [fieldDefs, getById, getName, getUnitName],
  );

  const archiveColDef = useMemo<ColDef>(
    () => ({
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
        { field: 'name', headerName: 'Наименование характеристики', minWidth: 280 },
        {
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
          headerName: 'Код',
          minWidth: 120,
          valueGetter: (p) => {
            const code = p.data?.properties?.code;
            return code != null ? String(code) : '';
          },
        },
        { field: 'name', headerName: 'Наименование', minWidth: 240 },
        ...propertyColumns,
        archiveColDef,
      ];
    }
    return [
      { field: 'name', headerName: 'Наименование', minWidth: 240 },
      ...propertyColumns,
      archiveColDef,
    ];
  }, [archiveColDef, config.resource, isPhysicalVolumeCatalog, optionCounts, propertyColumns]);

  const nameColumnDef = useMemo<ColDef<DirectoryTreeRow>>(
    () => ({ field: 'name', headerName: 'Наименование', minWidth: 240 }),
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

  const filteredTreeRows = useMemo(() => {
    if (config.resource !== 'equipment' && !isPhysicalVolumeCatalog) {
      return treeRows;
    }
    if (assessmentFilter === '') {
      return treeRows;
    }

    const matchesFilter = (row: DirectoryTreeRow): boolean => {
      if (assessmentFilter !== '') {
        const raw = row.properties.isAssessment;
        const val = raw === true || raw === 'true' ? 'true' : raw === false || raw === 'false' ? 'false' : '';
        if (val !== assessmentFilter) return false;
      }
      return true;
    };

    const byId = new Map(treeRows.map((r) => [r.id, r]));
    const visibleIds = new Set<number>();

    for (const row of treeRows) {
      if (!matchesFilter(row)) continue;
      visibleIds.add(row.id);
      let parentId = row.parentId;
      while (parentId != null) {
        visibleIds.add(parentId);
        parentId = byId.get(parentId)?.parentId ?? null;
      }
    }

    return treeRows.filter((row) => visibleIds.has(row.id));
  }, [assessmentFilter, config.resource, isPhysicalVolumeCatalog, treeRows]);

  const autoGroupColumnDef = useMemo<ColDef<DirectoryTreeRow>>(() => {
    if (isPhysicalVolumeCatalog) {
      return {
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
  const showFilters = isPhysicalVolumeCatalog || config.resource === 'equipment';

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0, flex: 1, overflow: 'hidden' }}>
      <DirectoryToolbar
        canEdit={canEdit}
        hasSelection={selected !== null}
        showExcel={config.showExcel !== false}
        showArchived={showArchived}
        onShowArchivedChange={setShowArchived}
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
        onExport={() => void exportDirectoryExcel(config.resource)}
      />

      {showFilters && (
        <Stack direction="row" spacing={2} sx={{ mb: 1.5, flexWrap: 'wrap' }}>
          <TextField
            select
            label="Оценка"
            size="small"
            value={assessmentFilter}
            onChange={(e) => setAssessmentFilter(e.target.value)}
            sx={{ minWidth: 120 }}
          >
            {ASSESSMENT_FILTER_OPTIONS.map((o) => (
              <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
            ))}
          </TextField>
        </Stack>
      )}

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Box sx={{ flex: 1, minHeight: 0, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        {config.isHierarchical ? (
          <DictionaryGrid<DirectoryTreeRow>
            key={`${gridId}-tree`}
            rowData={filteredTreeRows}
            columnDefs={treeColumnDefs}
            loading={loading}
            objectName={config.permission}
            gridId={gridId}
            treeData
            hierarchyField="hierarchy"
            autoGroupColumnDef={autoGroupColumnDef}
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
