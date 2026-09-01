import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Box, FormControl, InputLabel, MenuItem, Select, TextField } from '@mui/material';
import type { GridApi } from 'ag-grid-community';
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
import { fetchProjects } from '../../api/projectsApi';
import type { Project } from '../../types/project';
import { getApiErrorMessage } from '../../services/apiError';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import { HierarchicalSelect } from '../../components/dictionary/HierarchicalSelect';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { usePermission } from '../../hooks/usePermission';
import { useConfirmation } from '../../hooks/useConfirmation';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { CharacteristicEditDialog } from './CharacteristicEditDialog';
import { DirectoryEditDialog, DirectoryFormRow } from './DirectoryEditDialog';
import { DirectoryFormFields } from './DirectoryFormFields';
import { DirectoryToolbar } from './DirectoryToolbar';
import {
  buildPropertiesPayload,
  emptyProperties,
  getDirectoryFields,
  getFormFields,
  propertiesFromEntry,
} from './directoryFieldConfig';
import { validateDirectoryWrite } from './directoryWriteSchema';
import { z } from 'zod';
import type { ReferenceRouteConfig } from './referenceConfig';
import { isPhysicalVolumeCatalogResource } from './referenceConfig';
import { useDirectoryColumnDefs } from './useDirectoryColumnDefs';
import { formatAsIisNorm } from '../../utils/formatIisNorm';
import { resolveIicDirectionUnitName } from '../../utils/resolveIicDirectionUnit';
import { collectGridExportData } from '../../utils/directoryExport';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';

interface DirectoryPageProps {
  config: ReferenceRouteConfig;
}


export function DirectoryPage({ config }: DirectoryPageProps) {
  const { canRead, canEdit } = usePermission(config.permission);
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
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
  const isEquipmentCatalog = config.resource === 'equipment';

  const fieldDefs = useMemo(() => getDirectoryFields(config.resource), [config.resource]);
  const formFields = useMemo(() => getFormFields(config.resource), [config.resource]);

  const [mutating, setMutating] = useState(false);
  const [formError, setError] = useState<string | null>(null);
  const [showArchived, setShowArchived] = useState(false);
  const [equipmentProjects, setEquipmentProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState<number | ''>('');
  const showArchivedPrefKey = `references.${config.resource}.showArchived`;
  const equipmentProjectPrefKey = 'references.equipment.projectId';

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

  useEffect(() => {
    if (!isEquipmentCatalog) return;
    let cancelled = false;
    void (async () => {
      try {
        const [projects, prefs] = await Promise.all([fetchProjects(false), getUserPreferences()]);
        if (cancelled) return;
        setEquipmentProjects(projects);
        const saved = Number(prefs[equipmentProjectPrefKey]);
        if (Number.isFinite(saved) && projects.some((p) => p.id === saved)) {
          setSelectedProjectId(saved);
        } else if (projects.length > 0) {
          setSelectedProjectId(projects[0].id);
        }
      } catch {
        if (!cancelled) setEquipmentProjects([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [isEquipmentCatalog]);

  const handleShowArchivedChange = useCallback((value: boolean) => {
    setShowArchived(value);
    void putUserPreferences({ [showArchivedPrefKey]: value ? 'true' : 'false' });
  }, [showArchivedPrefKey]);

  const handleEquipmentProjectChange = useCallback((projectId: number | '') => {
    setSelectedProjectId(projectId);
    if (projectId !== '') {
      void putUserPreferences({ [equipmentProjectPrefKey]: String(projectId) });
    }
  }, []);
  const [selected, setSelected] = useState<DirectoryEntry | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<DirectoryEntry | null>(null);
  const [name, setName] = useState('');
  const [parentId, setParentId] = useState<number | ''>('');
  const [properties, setProperties] = useState<Record<string, unknown>>({});
  const [formIsArchived, setFormIsArchived] = useState(false);
  const [characteristicModalOpen, setCharacteristicModalOpen] = useState(false);
  const [characteristicEditing, setCharacteristicEditing] = useState<DirectoryEntry | null>(null);

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
    queryKey: ['directory-page', config.resource, showArchived, selectedProjectId],
    enabled: canRead && (!isEquipmentCatalog || selectedProjectId !== ''),
    staleTime: 30_000,
    queryFn: async () => {
      const projectId = isEquipmentCatalog && selectedProjectId !== '' ? selectedProjectId : null;
      if (config.isHierarchical) {
        const tree = await fetchDirectoryTree(config.resource, showArchived, projectId);
        const flattened = flattenDirectoryTree(tree, [], {
          hierarchyByCode: isPhysicalVolumeCatalogResource(config.resource),
        });
        return { kind: 'tree' as const, treeRows: flattened, rows: [] as DirectoryEntry[], optionCounts: {} as Record<number, number> };
      }

      const paged = await fetchDirectory(config.resource, showArchived, 1, 100, projectId);
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

  const treeRows = directoryQuery.data?.kind === 'tree' ? directoryQuery.data.treeRows : [];
  const rows = directoryQuery.data?.kind === 'flat' ? directoryQuery.data.rows : [];
  const optionCounts = directoryQuery.data?.kind === 'flat' ? directoryQuery.data.optionCounts : {};
  const loading = directoryQuery.isFetching;
  const queryError = directoryQuery.error
    ? (() => {
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
        return `${getApiErrorMessage(err, 'Не удалось загрузить справочник.')}${hint}`;
      })()
    : null;
  const error = formError ?? queryError;

  const { flatColumnDefs, treeColumnDefs, autoGroupColumnDef } = useDirectoryColumnDefs({
    resource: config.resource,
    isPhysicalVolumeCatalog,
    fieldDefs,
    optionCounts,
    getName,
    getById,
    getUnitName,
    resolveIicUnitName,
  });

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

  const openCreate = () => {
    if (isEquipmentCatalog && selectedProjectId === '') {
      setError('Выберите проект для справочника оборудования.');
      return;
    }
    setEditing(null);
    setName('');
    setParentId('');
    const props = emptyProperties(config.resource);
    if (isEquipmentCatalog && selectedProjectId !== '') {
      props.projectId = selectedProjectId;
    }
    setProperties(props);
    setFormIsArchived(false);
    setModalOpen(true);
  };

  const openEdit = (row: DirectoryEntry) => {
    setEditing(row);
    setName(row.name);
    setParentId(row.parentId ?? '');
    const props = propertiesFromEntry(config.resource, row.properties);
    if (isEquipmentCatalog) {
      props.projectId = row.properties.projectId ?? selectedProjectId;
    }
    setProperties(props);
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
    const payloadProps = buildPropertiesPayload(config.resource, properties);
    if (isEquipmentCatalog) {
      const projectId = Number(properties.projectId ?? selectedProjectId);
      if (!Number.isFinite(projectId) || projectId <= 0) {
        setError('Для оборудования обязателен проект.');
        return;
      }
      payloadProps.projectId = projectId;
    }
    const body = {
      name: name.trim(),
      parentId: parentId === '' ? null : Number(parentId),
      sortOrder: editing?.sortOrder ?? 0,
      properties: payloadProps,
    };
    try {
      validateDirectoryWrite(body);
    } catch (err) {
      setError(err instanceof z.ZodError ? err.issues[0]?.message ?? 'Некорректные данные формы.' : 'Некорректные данные формы.');
      return;
    }
    setMutating(true);
    try {
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
    } finally {
      setMutating(false);
    }
  };

  const handleDeleteForRow = async (row: DirectoryEntry) => {
    if (!(await confirm({
      title: 'Удалить запись?',
      description: `Удалить «${row.name}»?`,
    }))) return;
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
    if (!selected) return;
    if (!(await confirm({
      title: 'Удалить запись?',
      description: `Удалить «${selected.name}»?`,
    }))) return;
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
          config.resource === 'iic-work-directions'
            ? (item) =>
                formatAsIisNorm(
                  item.code,
                  item.name,
                  resolveIicUnitName(item.id),
                )
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
      <WorkingOverlay open={loading || mutating} delayMs={mutating ? 0 : 1000} />
      <Box sx={{ flexShrink: 0 }}>
        {isEquipmentCatalog && (
          <FormControl size="small" sx={{ minWidth: 280, mb: 1, mr: 1 }}>
            <InputLabel id="equipment-project-label">Проект</InputLabel>
            <Select
              labelId="equipment-project-label"
              label="Проект"
              value={selectedProjectId === '' ? '' : String(selectedProjectId)}
              onChange={(e) => {
                const raw = e.target.value;
                handleEquipmentProjectChange(raw === '' ? '' : Number(raw));
              }}
            >
              {equipmentProjects.map((p) => (
                <MenuItem key={p.id} value={String(p.id)}>
                  {p.code} — {p.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}
        <DirectoryToolbar
        canEdit={canEdit && (!isEquipmentCatalog || selectedProjectId !== '')}
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

      {isEquipmentCatalog && selectedProjectId === '' && (
        <Alert severity="info" sx={{ mb: 1, flexShrink: 0 }}>
          Выберите проект, чтобы просмотреть номенклатуру оборудования.
        </Alert>
      )}

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
      {ConfirmDialog}
    </Box>
  );
}
