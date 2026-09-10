import { useEffect, useMemo, useRef, useState } from 'react';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { buildVersionLabel } from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import {
  compareVersionAPrefKey,
  compareVersionBPrefKey,
  parsePrefId,
} from '../../../utils/projectWorkspacePrefs';
import { shortCatalogName } from '../forecast/catalogShortName';

export interface UseCompareVersionsParams {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspaceCompareVersionAId: number | null;
  workspaceCompareVersionBId: number | null;
}

export interface UseCompareVersionsResult {
  versionAId: number | null;
  versionBId: number | null;
  versionALabel: string;
  versionBLabel: string;
  catalogLabelA: string | null;
  catalogLabelB: string | null;
  applyVersion: (target: 'A' | 'B', version: ProjectVersion) => void;
}

function resolveCatalogShortLabel(
  version: ProjectVersion | undefined,
  physicalVolumeTypes: { id: number; name: string }[],
): string | null {
  const typeId = version?.physicalVolumeTypeId;
  if (!typeId) return null;
  const name = physicalVolumeTypes.find((t) => t.id === typeId)?.name ?? null;
  return shortCatalogName(name);
}

/** Пользовательский выбор версий А/Б сравнения, prefs и подписи каталогов. */
export function useCompareVersions({
  projectId,
  workspaceVersions,
  workspaceCompareVersionAId,
  workspaceCompareVersionBId,
}: UseCompareVersionsParams): UseCompareVersionsResult {
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const [userVersionAId, setUserVersionAId] = useState<number | null>(null);
  const [userVersionBId, setUserVersionBId] = useState<number | null>(null);
  const restoredComparePrefForProjectRef = useRef<number | null>(null);
  const versionAId = userVersionAId ?? workspaceCompareVersionAId;
  const versionBId = userVersionBId ?? workspaceCompareVersionBId;
  const versionALabel = userVersionAId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userVersionAId);
        return v ? buildVersionLabel(v) : '';
      })()
    : (() => {
        const v = workspaceVersions.find((vv) => vv.id === workspaceCompareVersionAId);
        return v ? buildVersionLabel(v) : '';
      })();
  const versionBLabel = userVersionBId != null
    ? (() => {
        const v = workspaceVersions.find((vv) => vv.id === userVersionBId);
        return v ? buildVersionLabel(v) : '';
      })()
    : (() => {
        const v = workspaceVersions.find((vv) => vv.id === workspaceCompareVersionBId);
        return v ? buildVersionLabel(v) : '';
      })();

  useEffect(() => {
    setUserVersionAId(null);
    setUserVersionBId(null);
    restoredComparePrefForProjectRef.current = null;
  }, [projectId]);

  useEffect(() => {
    if (workspaceVersions.length === 0 || restoredComparePrefForProjectRef.current === projectId) return;
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        restoredComparePrefForProjectRef.current = projectId;
        const savedA = parsePrefId(prefs[compareVersionAPrefKey(projectId)]);
        const savedB = parsePrefId(prefs[compareVersionBPrefKey(projectId)]);
        if (savedA != null && workspaceVersions.some((v) => v.id === savedA)) {
          setUserVersionAId(savedA);
        }
        if (savedB != null && workspaceVersions.some((v) => v.id === savedB)) {
          setUserVersionBId(savedB);
        }
      })
      .catch(() => {
        restoredComparePrefForProjectRef.current = projectId;
      });
    return () => {
      cancelled = true;
    };
  }, [projectId, workspaceVersions]);

  const applyVersion = (target: 'A' | 'B', version: ProjectVersion) => {
    if (target === 'A') {
      setUserVersionAId(version.id);
      void putUserPreferences({ [compareVersionAPrefKey(projectId)]: String(version.id) });
    } else {
      setUserVersionBId(version.id);
      void putUserPreferences({ [compareVersionBPrefKey(projectId)]: String(version.id) });
    }
  };

  const catalogLabelA = useMemo(
    () => resolveCatalogShortLabel(workspaceVersions.find((v) => v.id === versionAId), physicalVolumeTypes),
    [physicalVolumeTypes, versionAId, workspaceVersions],
  );
  const catalogLabelB = useMemo(
    () => resolveCatalogShortLabel(workspaceVersions.find((v) => v.id === versionBId), physicalVolumeTypes),
    [physicalVolumeTypes, versionBId, workspaceVersions],
  );

  return {
    versionAId,
    versionBId,
    versionALabel,
    versionBLabel,
    catalogLabelA,
    catalogLabelB,
    applyVersion,
  };
}
