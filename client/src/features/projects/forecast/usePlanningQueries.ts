import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  fetchPlanningParameters,
  type ProjectVersionPlanningParameter,
} from '../../../api/planningParametersApi';
import { fetchPerformance } from '../../../api/analyticsApi';
import { fetchProjectVersions, type ProjectVersion } from '../../../api/projectVersionsApi';
import { queryKeys } from '../../../services/queryKeys';
import type { PlanningScenario } from '../../../types/norms';
import { getApiErrorMessage } from '../../../services/apiError';
import {
  filterVisibleRows,
  isProjectRowEditable,
  projectRollup,
  weightedFactZ,
  Z_PARAMETER_LEVEL,
  type ZParameterLevel,
  type ZTriple,
} from './planningZLevels';
import { draftsFromRows, formatZDisplay, type ZDraftTriple } from './planningZFormat';
import { buildPlanningTreeRows, mapsFromPerformance, type FactZMaps } from './planningTree';
import type { UsePlanningParametersParams } from './planningParametersTypes';

const EMPTY_PLANNING_ROWS: ProjectVersionPlanningParameter[] = [];
const EMPTY_FACT_MAP: Map<number, number | null> = new Map();

export function usePlanningQueries({
  open,
  projectId,
  versionId,
  planningScenario,
  showFactColumn,
}: UsePlanningParametersParams) {
  const queryEnabled = open && !!versionId;
  const planningKey = queryKeys.forecast.planningParameters(projectId, versionId ?? undefined);
  const versionsKey = queryKeys.projectVersions(projectId);

  const [rows, setRows] = useState<ProjectVersionPlanningParameter[]>([]);
  const [scenario, setScenario] = useState<PlanningScenario>(planningScenario);
  const [zLevel, setZLevel] = useState<ZParameterLevel>(Z_PARAMETER_LEVEL.EachElement);
  const [zDrafts, setZDrafts] = useState<Record<number, ZDraftTriple>>({});
  const [projectDraft, setProjectDraft] = useState<ZTriple>({
    zPessimistic: 0, zBase: 0, zOptimistic: 0,
  });
  const [saveError, setSaveError] = useState<string | null>(null);
  const hydratedSessionRef = useRef('');

  const parametersQuery = useQuery({
    queryKey: planningKey,
    queryFn: () => fetchPlanningParameters(projectId, versionId!),
    enabled: queryEnabled,
    refetchOnWindowFocus: false,
  });
  const versionsQuery = useQuery({
    queryKey: versionsKey,
    queryFn: () => fetchProjectVersions(projectId),
    enabled: queryEnabled,
    refetchOnWindowFocus: false,
  });
  const performanceQuery = useQuery({
    queryKey: [...planningKey, 'performance'] as const,
    queryFn: async (): Promise<FactZMaps> => {
      try {
        const performance = await fetchPerformance(projectId, versionId!);
        return mapsFromPerformance(performance);
      } catch {
        return { zMap: new Map(), hoursMap: new Map() };
      }
    },
    enabled: queryEnabled && showFactColumn,
    refetchOnWindowFocus: false,
  });

  const versionMeta = useMemo(
    (): ProjectVersion | null =>
      (versionsQuery.data ?? []).find((version) => version.id === versionId) ?? null,
    [versionsQuery.data, versionId],
  );
  const factZByDirection = useMemo(
    () => (showFactColumn ? (performanceQuery.data?.zMap ?? EMPTY_FACT_MAP) : EMPTY_FACT_MAP),
    [showFactColumn, performanceQuery.data],
  );
  const factHoursByDirection = useMemo(
    () => (showFactColumn ? (performanceQuery.data?.hoursMap ?? EMPTY_FACT_MAP) : EMPTY_FACT_MAP),
    [showFactColumn, performanceQuery.data],
  );

  const loading =
    parametersQuery.isLoading
    || versionsQuery.isLoading
    || (showFactColumn && performanceQuery.isLoading);
  const loadError =
    parametersQuery.isError || versionsQuery.isError
      ? getApiErrorMessage(
        parametersQuery.error ?? versionsQuery.error,
        'Не удалось загрузить параметры планирования.',
      )
      : null;

  useEffect(() => {
    setScenario(planningScenario);
  }, [planningScenario, open]);

  useEffect(() => {
    const sessionKey = open && versionId ? `${projectId}:${versionId}` : '';
    if (!sessionKey) {
      hydratedSessionRef.current = '';
      return;
    }
    if (parametersQuery.isError || versionsQuery.isError) {
      setRows(EMPTY_PLANNING_ROWS);
      setZDrafts({});
      setProjectDraft({ zPessimistic: 0, zBase: 0, zOptimistic: 0 });
      hydratedSessionRef.current = '';
      return;
    }
    if (!parametersQuery.isSuccess || !versionsQuery.isSuccess) return;
    if (hydratedSessionRef.current === sessionKey) return;
    hydratedSessionRef.current = sessionKey;
    const data = parametersQuery.data ?? EMPTY_PLANNING_ROWS;
    setRows(data);
    if (versionMeta?.planningScenario) {
      setScenario(versionMeta.planningScenario as PlanningScenario);
    }
    setZLevel((versionMeta?.zParameterLevel ?? Z_PARAMETER_LEVEL.EachElement) as ZParameterLevel);
    const rollup = projectRollup(data);
    setProjectDraft(rollup);
    setZDrafts({
      ...draftsFromRows(data),
      0: {
        zPessimistic: formatZDisplay(rollup.zPessimistic),
        zBase: formatZDisplay(rollup.zBase),
        zOptimistic: formatZDisplay(rollup.zOptimistic),
      },
    });
    setSaveError(null);
  }, [
    open, projectId, versionId, parametersQuery.isSuccess, parametersQuery.isError, parametersQuery.data,
    versionsQuery.isSuccess, versionsQuery.isError, versionMeta,
  ]);

  const treeRows = useMemo(() => buildPlanningTreeRows(rows), [rows]);
  const visibleRows = useMemo(() => filterVisibleRows(treeRows, zLevel), [treeRows, zLevel]);
  const projectDisplay = useMemo((): ZTriple => {
    if (isProjectRowEditable(zLevel)) return projectDraft;
    return projectRollup(rows);
  }, [zLevel, projectDraft, rows]);
  const projectFactZ = useMemo(
    () => (showFactColumn ? weightedFactZ(rows, factZByDirection, factHoursByDirection) : null),
    [showFactColumn, rows, factZByDirection, factHoursByDirection],
  );

  const syncProjectDraftDisplay = useCallback((triple: ZTriple) => {
    setZDrafts((prev) => ({
      ...prev,
      0: {
        zPessimistic: formatZDisplay(triple.zPessimistic),
        zBase: formatZDisplay(triple.zBase),
        zOptimistic: formatZDisplay(triple.zOptimistic),
      },
    }));
  }, []);

  useEffect(() => {
    if (isProjectRowEditable(zLevel)) return;
    syncProjectDraftDisplay(projectRollup(rows));
  }, [rows, zLevel, syncProjectDraftDisplay]);

  return {
    planningKey,
    versionsKey,
    rows,
    setRows,
    scenario,
    setScenario,
    zLevel,
    setZLevel,
    zDrafts,
    setZDrafts,
    projectDraft,
    setProjectDraft,
    saveError,
    setSaveError,
    versionMeta,
    factZByDirection,
    visibleRows,
    projectDisplay,
    projectFactZ,
    loading,
    loadError,
    syncProjectDraftDisplay,
  };
}
