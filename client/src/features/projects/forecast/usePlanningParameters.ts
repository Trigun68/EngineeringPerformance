import { useCallback } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  savePlanningParameters,
  type ProjectVersionPlanningParameter,
} from '../../../api/planningParametersApi';
import { fetchProjectVersions, updateProjectVersion, type ProjectVersion } from '../../../api/projectVersionsApi';
import type { PlanningScenario } from '../../../types/norms';
import { getApiErrorMessage } from '../../../services/apiError';
import { usePermission } from '../../../hooks/usePermission';
import { OBJECT_NAME } from '../../../constants/accessControlConstants';
import {
  cascadeZ,
  isProjectRowEditable,
  projectRollup,
  Z_PARAMETER_LEVEL,
  type ZParameterLevel,
  type ZTriple,
} from './planningZLevels';
import { draftsFromRows, formatZDisplay, parseZInput, type ZDraftTriple } from './planningZFormat';
import { usePlanningQueries } from './usePlanningQueries';
import type { UsePlanningParametersParams } from './planningParametersTypes';

export type { UsePlanningParametersParams };
export type ZField = 'zPessimistic' | 'zBase' | 'zOptimistic';

interface SavePlanningVariables {
  committed: ProjectVersionPlanningParameter[];
  scenario: PlanningScenario;
  zLevel: ZParameterLevel;
}

export function usePlanningParameters(params: UsePlanningParametersParams) {
  const { projectId, versionId, onClose, onSaved } = params;
  const { canEdit } = usePermission(OBJECT_NAME.Planning);
  const queryClient = useQueryClient();
  const q = usePlanningQueries(params);
  const {
    planningKey, versionsKey, rows, setRows, scenario, setScenario, zLevel, setZLevel,
    zDrafts, setZDrafts, projectDraft, setProjectDraft, saveError, setSaveError,
    versionMeta, factZByDirection, visibleRows, projectDisplay, projectFactZ,
    loading, loadError, syncProjectDraftDisplay,
  } = q;

  const applyCascade = (source: 'project' | number, z: ZTriple) => {
    setRows((prev) => {
      const next = cascadeZ(prev, source, z);
      setZDrafts((drafts) => {
        const formatted = draftsFromRows(next);
        const nextDrafts: Record<number, ZDraftTriple> = { ...formatted };
        if (source === 'project') {
          if (drafts[0]) nextDrafts[0] = drafts[0];
        } else {
          if (drafts[source]) nextDrafts[source] = drafts[source];
          if (drafts[0]) nextDrafts[0] = drafts[0];
        }
        return nextDrafts;
      });
      return next;
    });
  };

  const emptyDraft = (): ZDraftTriple => ({
    zPessimistic: formatZDisplay(0), zBase: formatZDisplay(0), zOptimistic: formatZDisplay(0),
  });

  const updateDirectionField = (directionId: number, field: ZField, raw: string) => {
    setZDrafts((prev) => ({
      ...prev,
      [directionId]: { ...(prev[directionId] ?? emptyDraft()), [field]: raw },
    }));
    const value = parseZInput(raw);
    if (value === null) return;
    const current = rows.find((r) => r.iicWorkDirectionId === directionId);
    if (!current) return;
    const nextZ: ZTriple = {
      zPessimistic: field === 'zPessimistic' ? value : current.zPessimistic,
      zBase: field === 'zBase' ? value : current.zBase,
      zOptimistic: field === 'zOptimistic' ? value : current.zOptimistic,
    };
    if (zLevel === Z_PARAMETER_LEVEL.EachElement) {
      setRows((prev) =>
        prev.map((row) => (row.iicWorkDirectionId === directionId ? { ...row, [field]: value } : row)),
      );
    } else {
      applyCascade(directionId, nextZ);
    }
  };

  const updateProjectField = (field: ZField, raw: string) => {
    setZDrafts((prev) => ({
      ...prev,
      0: { ...(prev[0] ?? emptyDraft()), [field]: raw },
    }));
    const value = parseZInput(raw);
    if (value === null) return;
    const next: ZTriple = {
      zPessimistic: field === 'zPessimistic' ? value : projectDraft.zPessimistic,
      zBase: field === 'zBase' ? value : projectDraft.zBase,
      zOptimistic: field === 'zOptimistic' ? value : projectDraft.zOptimistic,
    };
    setProjectDraft(next);
    applyCascade('project', next);
  };

  const blurDirection = (directionId: number, field: ZField) => {
    setZDrafts((prev) => {
      const draft = prev[directionId];
      if (!draft) return prev;
      const value = parseZInput(draft[field]);
      if (value === null) return prev;
      return { ...prev, [directionId]: { ...draft, [field]: formatZDisplay(value) } };
    });
  };

  const blurProject = (field: ZField) => {
    setZDrafts((prev) => {
      const draft = prev[0];
      if (!draft) return prev;
      const value = parseZInput(draft[field]);
      if (value === null) return prev;
      return { ...prev, 0: { ...draft, [field]: formatZDisplay(value) } };
    });
  };

  const handleZLevelChange = (next: ZParameterLevel) => {
    setZLevel(next);
    const rollup = projectRollup(rows);
    setProjectDraft(rollup);
    syncProjectDraftDisplay(rollup);
  };

  const commitDraftsToRows = useCallback((): ProjectVersionPlanningParameter[] => {
    let merged = rows.map((row) => {
      const draft = zDrafts[row.iicWorkDirectionId];
      if (!draft) return row;
      return {
        ...row,
        zPessimistic: parseZInput(draft.zPessimistic) ?? row.zPessimistic,
        zBase: parseZInput(draft.zBase) ?? row.zBase,
        zOptimistic: parseZInput(draft.zOptimistic) ?? row.zOptimistic,
      };
    });
    if (isProjectRowEditable(zLevel)) {
      const pd = zDrafts[0];
      const z: ZTriple = {
        zPessimistic: (pd ? parseZInput(pd.zPessimistic) : null) ?? projectDraft.zPessimistic,
        zBase: (pd ? parseZInput(pd.zBase) : null) ?? projectDraft.zBase,
        zOptimistic: (pd ? parseZInput(pd.zOptimistic) : null) ?? projectDraft.zOptimistic,
      };
      merged = cascadeZ(merged, 'project', z);
    }
    return merged;
  }, [rows, zDrafts, zLevel, projectDraft]);

  const saveMutation = useMutation({
    mutationFn: async ({ committed, scenario: nextScenario, zLevel: nextLevel }: SavePlanningVariables) => {
      if (!versionId) throw new Error('Не указана версия анализа.');
      await savePlanningParameters(
        projectId,
        versionId,
        committed.map((row) => ({
          iicWorkDirectionId: row.iicWorkDirectionId,
          zPessimistic: row.zPessimistic,
          zBase: row.zBase,
          zOptimistic: row.zOptimistic,
        })),
      );
      let meta =
        queryClient.getQueryData<ProjectVersion[]>(versionsKey)?.find((v) => v.id === versionId)
        ?? versionMeta;
      if (!meta) {
        const versions = await fetchProjectVersions(projectId);
        meta = versions.find((v) => v.id === versionId) ?? null;
      }
      if (!meta) {
        throw new Error('Не удалось загрузить карточку версии для сохранения сценария и уровня Z.');
      }
      await updateProjectVersion(projectId, versionId, {
        versionName: meta.versionName,
        description: meta.description ?? null,
        direction: meta.direction,
        physicalVolumeTypeId: meta.physicalVolumeTypeId ?? null,
        planningScenario: nextScenario,
        zParameterLevel: nextLevel,
        rowVersion: meta.rowVersion ?? null,
      });
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: planningKey }),
        queryClient.invalidateQueries({ queryKey: versionsKey }),
      ]);
      await Promise.all([
        queryClient.refetchQueries({ queryKey: planningKey }),
        queryClient.refetchQueries({ queryKey: versionsKey }),
      ]);
      onSaved?.();
      onClose();
    },
    onError: (err) => {
      setSaveError(getApiErrorMessage(err, 'Не удалось сохранить параметры планирования.'));
    },
  });

  const saving = saveMutation.isPending;

  const handleSave = () => {
    if (!versionId || !canEdit || saving) return;
    setSaveError(null);
    const committed = commitDraftsToRows();
    setRows(committed);
    setZDrafts({
      ...draftsFromRows(committed),
      0: zDrafts[0]
        ? {
            zPessimistic: formatZDisplay(parseZInput(zDrafts[0].zPessimistic) ?? projectDraft.zPessimistic),
            zBase: formatZDisplay(parseZInput(zDrafts[0].zBase) ?? projectDraft.zBase),
            zOptimistic: formatZDisplay(parseZInput(zDrafts[0].zOptimistic) ?? projectDraft.zOptimistic),
          }
        : {
            zPessimistic: formatZDisplay(projectDraft.zPessimistic),
            zBase: formatZDisplay(projectDraft.zBase),
            zOptimistic: formatZDisplay(projectDraft.zOptimistic),
          },
    });
    saveMutation.mutate({ committed, scenario, zLevel });
  };

  return {
    canEdit, scenario, setScenario, zLevel, handleZLevelChange, zDrafts, visibleRows,
    projectDisplay, projectFactZ, factZByDirection, loading, error: saveError ?? loadError,
    saving, handleSave, updateDirectionField, updateProjectField, blurDirection, blurProject,
    projectEditable: canEdit && isProjectRowEditable(zLevel),
  };
}
