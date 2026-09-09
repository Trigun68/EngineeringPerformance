import { useEffect, useMemo, useRef, useState } from 'react';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { buildVersionLabel } from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import {
  LAST_PROJECT_PREF_KEY,
  factVersionPrefKey,
  parsePrefId,
} from '../../../utils/projectWorkspacePrefs';
import { shortCatalogName } from '../forecast/catalogShortName';

export interface UseFactVersionParams {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspaceFactVersionId: number | null;
  workspaceFactVersionLabel: string;
}

export interface UseFactVersionResult {
  factVersionId: number | null;
  factVersionLabel: string;
  catalogShortLabel: string | null;
  catalogTypeMissing: boolean;
  importBlocked: boolean;
  handleVersionSelect: (version: ProjectVersion) => void;
}

/** Пользовательский выбор версии факта, prefs и подпись каталога. */
export function useFactVersion({
  projectId,
  workspaceVersions,
  workspaceFactVersionId,
  workspaceFactVersionLabel,
}: UseFactVersionParams): UseFactVersionResult {
  const [userFactVersionId, setUserFactVersionId] = useState<number | null>(null);
  const restoredFactPrefForProjectRef = useRef<number | null>(null);
  const factVersionId = userFactVersionId ?? workspaceFactVersionId;
  const factVersionLabel = userFactVersionId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userFactVersionId);
        return v ? buildVersionLabel(v) : '';
      })()
    : workspaceFactVersionLabel;

  const selectedFactVersion = useMemo(
    () => workspaceVersions.find((v) => v.id === factVersionId) ?? null,
    [workspaceVersions, factVersionId],
  );
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const catalogTypeName = useMemo(() => {
    const typeId = selectedFactVersion?.physicalVolumeTypeId;
    if (!typeId) return null;
    return physicalVolumeTypes.find((t) => t.id === typeId)?.name ?? null;
  }, [physicalVolumeTypes, selectedFactVersion?.physicalVolumeTypeId]);
  const catalogShortLabel = useMemo(() => shortCatalogName(catalogTypeName), [catalogTypeName]);
  const catalogTypeMissing = selectedFactVersion != null && !selectedFactVersion.physicalVolumeTypeId;
  const importBlocked = !factVersionId || catalogTypeMissing;

  useEffect(() => {
    setUserFactVersionId(null);
    restoredFactPrefForProjectRef.current = null;
  }, [projectId]);

  useEffect(() => {
    if (workspaceVersions.length === 0 || restoredFactPrefForProjectRef.current === projectId) return;
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        restoredFactPrefForProjectRef.current = projectId;
        const saved = parsePrefId(prefs[factVersionPrefKey(projectId)]);
        if (saved != null && workspaceVersions.some((v) => v.id === saved)) {
          setUserFactVersionId(saved);
        }
      })
      .catch(() => {
        restoredFactPrefForProjectRef.current = projectId;
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, workspaceVersions]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setUserFactVersionId(version.id);
    void putUserPreferences({
      [LAST_PROJECT_PREF_KEY]: String(projectId),
      [factVersionPrefKey(projectId)]: String(version.id),
    });
  };

  return {
    factVersionId,
    factVersionLabel,
    catalogShortLabel,
    catalogTypeMissing,
    importBlocked,
    handleVersionSelect,
  };
}
