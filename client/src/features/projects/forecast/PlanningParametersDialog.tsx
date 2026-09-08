import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import {
  fetchPlanningParameters,
  savePlanningParameters,
  type ProjectVersionPlanningParameter,
} from '../../../api/planningParametersApi';
import { fetchPerformance } from '../../../api/analyticsApi';
import { fetchProjectVersions, updateProjectVersion, type ProjectVersion } from '../../../api/projectVersionsApi';
import { queryKeys } from '../../../services/queryKeys';
import { PlanningScenarioSelector } from '../../norms/PlanningScenarioSelector';
import { ZParametersCaption } from './ZParametersCaption';
import type { PlanningScenario } from '../../../types/norms';
import { getApiErrorMessage } from '../../../services/apiError';
import { usePermission } from '../../../hooks/usePermission';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import {
  cascadeZ,
  filterVisibleRows,
  isDirectionEditable,
  isProjectRowEditable,
  projectRollup,
  weightedFactZ,
  Z_PARAMETER_LEVEL,
  Z_PARAMETER_LEVEL_LABELS,
  type ZParameterLevel,
  type ZTriple,
} from './planningZLevels';

export interface PlanningParametersDialogProps {
  open: boolean;
  projectId: number;
  versionId: number | null;
  planningScenario?: PlanningScenario;
  /** Read-only столбец «Факт» (Z из performance текущей версии) — для вкладки «Оценка факта». */
  showFactColumn?: boolean;
  onClose: () => void;
  onSaved?: () => void;
}

interface PlanningTreeRow extends ProjectVersionPlanningParameter {
  depth: number;
  hasChildren: boolean;
}

type ZField = 'zPessimistic' | 'zBase' | 'zOptimistic';

const EMPTY_PLANNING_ROWS: ProjectVersionPlanningParameter[] = [];
const EMPTY_FACT_MAP: Map<number, number | null> = new Map();

interface FactZMaps {
  zMap: Map<number, number | null>;
  hoursMap: Map<number, number | null>;
}

interface SavePlanningVariables {
  committed: ProjectVersionPlanningParameter[];
  scenario: PlanningScenario;
  zLevel: ZParameterLevel;
}

function mapsFromPerformance(performance: Awaited<ReturnType<typeof fetchPerformance>>): FactZMaps {
  const zMap = new Map<number, number | null>();
  const hoursMap = new Map<number, number | null>();
  for (const row of performance) {
    if (row.isTotal) continue;
    zMap.set(row.workDirectionId, row.zScore);
    hoursMap.set(row.workDirectionId, row.factLaborHours);
  }
  return { zMap, hoursMap };
}

function buildPlanningTreeRows(rows: ProjectVersionPlanningParameter[]): PlanningTreeRow[] {
  const parentIds = new Set(rows.map((r) => r.parentId).filter((id): id is number => id != null));
  const byParent = rows.reduce<Map<number | null, ProjectVersionPlanningParameter[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  for (const list of byParent.values()) {
    list.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru'));
  }

  const result: PlanningTreeRow[] = [];
  const walk = (parentId: number | null, depth: number) => {
    for (const row of byParent.get(parentId) ?? []) {
      result.push({
        ...row,
        depth,
        hasChildren: parentIds.has(row.iicWorkDirectionId),
      });
      walk(row.iicWorkDirectionId, depth + 1);
    }
  };
  walk(null, 0);
  return result;
}

export function formatZDisplay(value: number): string {
  return Number.isFinite(value) ? value.toFixed(2).replace('.', ',') : '0,00';
}

/** Парсит Z с точки или запятой; незавершённый ввод («», «-», «,») → null. */
export function parseZInput(raw: string): number | null {
  const normalized = raw.trim().replace(/\s/g, '').replace(',', '.');
  if (
    normalized === ''
    || normalized === '-'
    || normalized === '.'
    || normalized === '-.'
  ) {
    return null;
  }
  const value = Number(normalized);
  return Number.isFinite(value) ? value : null;
}

function draftsFromRows(
  rows: ProjectVersionPlanningParameter[],
): Record<number, { zPessimistic: string; zBase: string; zOptimistic: string }> {
  return Object.fromEntries(
    rows.map((row) => [
      row.iicWorkDirectionId,
      {
        zPessimistic: formatZDisplay(row.zPessimistic),
        zBase: formatZDisplay(row.zBase),
        zOptimistic: formatZDisplay(row.zOptimistic),
      },
    ]),
  );
}

export function PlanningParametersDialog({
  open,
  projectId,
  versionId,
  planningScenario = 1,
  showFactColumn = false,
  onClose,
  onSaved,
}: PlanningParametersDialogProps) {
  const { canEdit } = usePermission('Planning');
  const queryClient = useQueryClient();
  const queryEnabled = open && !!versionId;
  const planningKey = queryKeys.forecast.planningParameters(projectId, versionId ?? undefined);
  const versionsKey = queryKeys.projectVersions(projectId);

  const [rows, setRows] = useState<ProjectVersionPlanningParameter[]>([]);
  const [scenario, setScenario] = useState<PlanningScenario>(planningScenario);
  const [zLevel, setZLevel] = useState<ZParameterLevel>(Z_PARAMETER_LEVEL.EachElement);
  /** Черновики полей по iicWorkDirectionId; ключ 0 — виртуальная строка «По проекту». */
  const [zDrafts, setZDrafts] = useState<
    Record<number, { zPessimistic: string; zBase: string; zOptimistic: string }>
  >({});
  const [projectDraft, setProjectDraft] = useState<ZTriple>({
    zPessimistic: 0,
    zBase: 0,
    zOptimistic: 0,
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
  const error =
    saveError
    ?? (parametersQuery.isError || versionsQuery.isError
      ? getApiErrorMessage(
        parametersQuery.error ?? versionsQuery.error,
        'Не удалось загрузить параметры планирования.',
      )
      : null);

  useEffect(() => {
    setScenario(planningScenario);
  }, [planningScenario, open]);

  // Локальные черновики Z гидрируем один раз на открытие диалога, чтобы refetch не затирал ввод.
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
    open,
    projectId,
    versionId,
    parametersQuery.isSuccess,
    parametersQuery.isError,
    parametersQuery.data,
    versionsQuery.isSuccess,
    versionsQuery.isError,
    versionMeta,
  ]);

  const treeRows = useMemo(() => buildPlanningTreeRows(rows), [rows]);
  const visibleRows = useMemo(() => filterVisibleRows(treeRows, zLevel), [treeRows, zLevel]);

  const projectDisplay = useMemo((): ZTriple => {
    if (isProjectRowEditable(zLevel)) {
      return projectDraft;
    }
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

  // При не-ByProject обновляем отображение rollup «по проекту» из листьев.
  useEffect(() => {
    if (isProjectRowEditable(zLevel)) return;
    syncProjectDraftDisplay(projectRollup(rows));
  }, [rows, zLevel, syncProjectDraftDisplay]);

  const applyCascade = (source: 'project' | number, z: ZTriple) => {
    setRows((prev) => {
      const next = cascadeZ(prev, source, z);
      // Не затирать строку, которую сейчас редактируют (иначе «1» → «1,00» и нельзя ввести дробь).
      setZDrafts((drafts) => {
        const formatted = draftsFromRows(next);
        const nextDrafts: Record<number, { zPessimistic: string; zBase: string; zOptimistic: string }> = {
          ...formatted,
        };
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

  const updateDirectionField = (directionId: number, field: ZField, raw: string) => {
    setZDrafts((prev) => ({
      ...prev,
      [directionId]: { ...(prev[directionId] ?? {
        zPessimistic: formatZDisplay(0),
        zBase: formatZDisplay(0),
        zOptimistic: formatZDisplay(0),
      }), [field]: raw },
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

    // На уровнях L1/L2 правка узла каскадирует в потомков; на EachElement — только сам узел.
    if (zLevel === Z_PARAMETER_LEVEL.EachElement) {
      setRows((prev) =>
        prev.map((row) =>
          row.iicWorkDirectionId === directionId ? { ...row, [field]: value } : row,
        ),
      );
    } else {
      applyCascade(directionId, nextZ);
    }
  };

  const updateProjectField = (field: ZField, raw: string) => {
    setZDrafts((prev) => ({
      ...prev,
      0: { ...(prev[0] ?? {
        zPessimistic: formatZDisplay(0),
        zBase: formatZDisplay(0),
        zOptimistic: formatZDisplay(0),
      }), [field]: raw },
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
      return {
        ...prev,
        [directionId]: { ...draft, [field]: formatZDisplay(value) },
      };
    });
  };

  const blurProject = (field: ZField) => {
    setZDrafts((prev) => {
      const draft = prev[0];
      if (!draft) return prev;
      const value = parseZInput(draft[field]);
      if (value === null) return prev;
      return {
        ...prev,
        0: { ...draft, [field]: formatZDisplay(value) },
      };
    });
  };

  const handleZLevelChange = (next: ZParameterLevel) => {
    setZLevel(next);
    if (!isProjectRowEditable(next)) {
      const rollup = projectRollup(rows);
      setProjectDraft(rollup);
      syncProjectDraftDisplay(rollup);
    } else {
      // Переход на «по проекту»: показываем текущий rollup как стартовые editable значения.
      const rollup = projectRollup(rows);
      setProjectDraft(rollup);
      syncProjectDraftDisplay(rollup);
    }
  };

  /** Перед сохранением: перенести черновики полей в rows (и каскад «по проекту»). */
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
      if (!versionId) {
        throw new Error('Не указана версия анализа.');
      }
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

  const projectEditable = canEdit && isProjectRowEditable(zLevel);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth data-testid="planning-parameters-dialog">
      <WorkingOverlay open={saving || loading} delayMs={saving ? 0 : 1000} />
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        Параметры планирования
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <ZParametersCaption
          variant={showFactColumn ? 'fact' : 'plan'}
          planVersionSelected={Boolean(versionId)}
        />
        <Box sx={{ mb: 2, maxWidth: 420, display: 'flex', flexDirection: 'column', gap: 2 }}>
          <PlanningScenarioSelector
            value={scenario}
            onChange={setScenario}
            disabled={!canEdit || saving}
            helperText="Группировка объёма включает физические объёмы и оборудование одной группы ИИС."
          />
          <TextField
            select
            size="small"
            fullWidth
            label="Уровень параметров Z-оценки"
            value={zLevel}
            disabled={!canEdit || saving}
            onChange={(e) => handleZLevelChange(Number(e.target.value) as ZParameterLevel)}
            data-testid="z-parameter-level-selector"
          >
            {(Object.entries(Z_PARAMETER_LEVEL_LABELS) as Array<[string, string]>).map(([k, label]) => (
              <MenuItem key={k} value={Number(k)}>
                {label}
              </MenuItem>
            ))}
          </TextField>
        </Box>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={28} />
          </Box>
        ) : (
          <TableContainer component={Paper} variant="outlined">
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>Группа физ. объёмов IIC</TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>
                    Пессимист.
                  </TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>
                    Базовый
                  </TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>
                    Оптимист.
                  </TableCell>
                  {showFactColumn && (
                    <TableCell align="right" sx={{ width: 120 }}>
                      Факт
                    </TableCell>
                  )}
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow data-testid="z-project-row">
                  <TableCell sx={{ fontWeight: 600 }}>По проекту</TableCell>
                  {(['zPessimistic', 'zBase', 'zOptimistic'] as const).map((field) => (
                    <TableCell key={field} align="right">
                      <TextField
                        size="small"
                        type="text"
                        inputMode="decimal"
                        value={
                          zDrafts[0]?.[field] ?? formatZDisplay(projectDisplay[field])
                        }
                        disabled={!projectEditable}
                        onChange={(e) => updateProjectField(field, e.target.value)}
                        onBlur={() => blurProject(field)}
                        inputProps={{
                          style: { textAlign: 'right' },
                          readOnly: !projectEditable,
                          'aria-label': `Z ${field} по проекту`,
                        }}
                        sx={{ width: 96 }}
                      />
                    </TableCell>
                  ))}
                  {showFactColumn && (
                    <TableCell align="right" data-testid="z-project-fact">
                      <TextField
                        size="small"
                        value={projectFactZ != null ? formatZDisplay(projectFactZ) : '—'}
                        disabled
                        inputProps={{
                          style: { textAlign: 'right' },
                          readOnly: true,
                          'aria-label': 'Факт Z по проекту',
                        }}
                        sx={{ width: 96 }}
                      />
                    </TableCell>
                  )}
                </TableRow>
                {visibleRows.map((row) => {
                  const editable = canEdit && isDirectionEditable(zLevel, row.depth);
                  const factZ = factZByDirection.get(row.iicWorkDirectionId);
                  const factDisplay =
                    factZ != null && Number.isFinite(factZ)
                      ? formatZDisplay(factZ)
                      : row.zActual != null
                        ? formatZDisplay(row.zActual)
                        : '—';
                  return (
                    <TableRow key={row.id}>
                      <TableCell sx={{ pl: 2 + row.depth * 2.5 }}>
                        {formatIisNorm({ code: row.code, name: row.name, unitName: row.unitName })}
                      </TableCell>
                      {(['zPessimistic', 'zBase', 'zOptimistic'] as const).map((field) => (
                        <TableCell key={field} align="right">
                          <TextField
                            size="small"
                            type="text"
                            inputMode="decimal"
                            value={
                              zDrafts[row.iicWorkDirectionId]?.[field] ?? formatZDisplay(row[field])
                            }
                            disabled={!editable}
                            onChange={(e) =>
                              updateDirectionField(row.iicWorkDirectionId, field, e.target.value)
                            }
                            onBlur={() => blurDirection(row.iicWorkDirectionId, field)}
                            inputProps={{
                              style: { textAlign: 'right' },
                              'aria-label': `Z ${field} ${row.code}`,
                            }}
                            sx={{ width: 96 }}
                          />
                        </TableCell>
                      ))}
                      {showFactColumn && (
                        <TableCell align="right">
                          <TextField
                            size="small"
                            value={factDisplay}
                            disabled
                            inputProps={{
                              style: { textAlign: 'right' },
                              readOnly: true,
                              'aria-label': `Факт Z ${row.name}`,
                            }}
                            sx={{ width: 96 }}
                          />
                        </TableCell>
                      )}
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Закрыть</Button>
        {canEdit && (
          <Button variant="contained" onClick={() => void handleSave()} disabled={saving || loading || !versionId}>
            {saving ? 'Сохранение…' : 'Сохранить'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
