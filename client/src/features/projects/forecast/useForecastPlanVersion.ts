import { useEffect, useMemo, useRef, useState } from 'react';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { buildVersionLabel } from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import {
  LAST_PROJECT_PREF_KEY,
  parsePrefId,
  planVersionPrefKey,
} from '../../../utils/projectWorkspacePrefs';
import { shortCatalogName } from './catalogShortName';

export interface UseForecastPlanVersionParams {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspacePlanVersionId: number | null;
  workspacePlanVersionLabel: string;
}

export interface UseForecastPlanVersionResult {
  planVersionId: number | null;
  planVersionLabel: string;
  catalogShortLabel: string | null;
  handleVersionSelect: (version: ProjectVersion) => void;
}

/** Пользовательский выбор версии плана, prefs и подпись каталога. */
export function useForecastPlanVersion({
  projectId,
  workspaceVersions,
  workspacePlanVersionId,
  workspacePlanVersionLabel,
}: UseForecastPlanVersionParams): UseForecastPlanVersionResult {
  const [userPlanVersionId, setUserPlanVersionId] = useState<number | null>(null);
  const restoredPlanPrefForProjectRef = useRef<number | null>(null);
  const planVersionId = userPlanVersionId ?? workspacePlanVersionId;
  const planVersionLabel = userPlanVersionId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userPlanVersionId);
        return v ? buildVersionLabel(v) : '';
      })()
    : workspacePlanVersionLabel;

  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);

  const selectedPlanVersion = useMemo(
    () => workspaceVersions.find((v) => v.id === planVersionId) ?? null,
    [workspaceVersions, planVersionId],
  );

  const catalogShortLabel = useMemo(() => {
    const typeId = selectedPlanVersion?.physicalVolumeTypeId;
    if (!typeId) return null;
    const full = physicalVolumeTypes.find((t) => t.id === typeId)?.name
      ?? physicalVolumeTypes.find((t) => t.id === typeId)?.description
      ?? null;
    return shortCatalogName(full);
  }, [physicalVolumeTypes, selectedPlanVersion?.physicalVolumeTypeId]);

  useEffect(() => {
    setUserPlanVersionId(null);
    restoredPlanPrefForProjectRef.current = null;
  }, [projectId]);

  useEffect(() => {
    if (workspaceVersions.length === 0 || restoredPlanPrefForProjectRef.current === projectId) return;
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        restoredPlanPrefForProjectRef.current = projectId;
        const saved = parsePrefId(prefs[planVersionPrefKey(projectId)]);
        if (saved != null && workspaceVersions.some((v) => v.id === saved)) {
          setUserPlanVersionId(saved);
        }
      })
      .catch(() => {
        restoredPlanPrefForProjectRef.current = projectId;
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, workspaceVersions]);

  const handleVersionSelect = (version: ProjectVersion) => {
    setUserPlanVersionId(version.id);
    void putUserPreferences({
      [LAST_PROJECT_PREF_KEY]: String(projectId),
      [planVersionPrefKey(projectId)]: String(version.id),
    });
  };

  return {
    planVersionId,
    planVersionLabel,
    catalogShortLabel,
    handleVersionSelect,
  };
}
