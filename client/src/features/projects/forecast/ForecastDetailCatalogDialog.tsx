import { useEffect, useMemo, useState } from 'react';
import { TextField } from '@mui/material';
import {
  fetchDirectoryEntry,
  updateDirectoryEntry,
  type DirectoryEntry,
} from '../../../api/directoriesApi';
import {
  buildPropertiesPayload,
  getFormFields,
  propertiesFromEntry,
} from '../../../pages/references/directoryFieldConfig';
import { DirectoryEditDialog, DirectoryFormRow } from '../../../pages/references/DirectoryEditDialog';
import { DirectoryFormFields } from '../../../pages/references/DirectoryFormFields';
import { isPhysicalVolumeCatalogResource } from '../../../pages/references/referenceConfig';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';

export interface ForecastDetailCatalogDialogProps {
  open: boolean;
  resource: string;
  entryId: number | null;
  title: string;
  onClose: () => void;
  onSaved: () => void;
}

export function ForecastDetailCatalogDialog({
  open,
  resource,
  entryId,
  title,
  onClose,
  onSaved,
}: ForecastDetailCatalogDialogProps) {
  const [entry, setEntry] = useState<DirectoryEntry | null>(null);
  const [name, setName] = useState('');
  const [properties, setProperties] = useState<Record<string, unknown>>({});
  const [isArchived, setIsArchived] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const fieldDefs = useMemo(() => getFormFields(resource), [resource]);
  const isPhysicalVolumeCatalog = isPhysicalVolumeCatalogResource(resource);

  useEffect(() => {
    if (!open || !entryId) {
      setEntry(null);
      return;
    }
    void fetchDirectoryEntry(resource, entryId)
      .then((loaded) => {
        setEntry(loaded);
        setName(loaded.name);
        setProperties(propertiesFromEntry(resource, loaded.properties as Record<string, unknown>));
        setIsArchived(loaded.isArchived);
      })
      .catch(() => setEntry(null));
  }, [entryId, open, resource]);

  const handleSubmit = async () => {
    if (!entryId) return;
    setSubmitting(true);
    try {
      const payload = buildPropertiesPayload(resource, properties);
      if (resource === 'equipment') {
        const projectId = Number(entry?.properties?.projectId ?? properties.projectId);
        if (Number.isFinite(projectId) && projectId > 0) {
          payload.projectId = projectId;
        }
      }
      await updateDirectoryEntry(resource, entryId, {
        name,
        parentId: entry?.parentId ?? null,
        sortOrder: entry?.sortOrder,
        properties: payload,
      });
      onSaved();
      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <WorkingOverlay open={submitting} />
      <DirectoryEditDialog
        open={open}
        title={title}
        onClose={onClose}
        onSubmit={handleSubmit}
        maxWidth="md"
      >
      {isPhysicalVolumeCatalog && (
        <DirectoryFormRow label="Код">
          <TextField
            value={String(properties.code ?? '')}
            onChange={(e) => setProperties((p) => ({ ...p, code: e.target.value }))}
            fullWidth
            size="small"
            required
          />
        </DirectoryFormRow>
      )}
      <DirectoryFormRow label="Наименование">
        <TextField value={name} onChange={(e) => setName(e.target.value)} fullWidth size="small" required />
      </DirectoryFormRow>
      <DirectoryFormFields
        fields={fieldDefs.filter((f) => f.showInForm !== false && f.key !== 'code')}
        properties={properties}
        onPropertyChange={(key, value) => setProperties((prev) => ({ ...prev, [key]: value }))}
        isArchived={isArchived}
        onIsArchivedChange={setIsArchived}
        showArchive
      />
    </DirectoryEditDialog>
    </>
  );
}
