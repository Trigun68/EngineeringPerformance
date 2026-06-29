import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Select,
  Stack,
  Tab,
  Tabs,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import {
  calculateForecast,
  fetchForecast,
  fetchPerformance,
  type ForecastCalculationOptions,
  type ForecastIicVolumeRow,
  type PerformanceRow,
  type ProjectForecastReport,
} from '../../api/analyticsApi';
import { importProjectData } from '../../api/importApi';
import {
  copyProjectVersion,
  createProjectVersion,
  deleteProjectVersion,
  fetchProjectVersions,
  updateProjectVersion,
  type ProjectVersion,
} from '../../api/projectVersionsApi';
import { DictionaryGrid } from '../../components/dictionary/DictionaryGrid';
import { usePermission } from '../../hooks/usePermission';

type ForecastMethod = 'regression' | 'element';
type DataViewTab = 'plan' | 'fact' | 'forecast';

const ANALYSIS_PLAN = 1;
const ANALYSIS_FACT = 2;
const ANALYSIS_FORECAST = 3;

interface ForecastPageProps {
  projectId: number;
}

interface VolumeTreeRow extends ForecastIicVolumeRow {
  hierarchy: string[];
  id: number;
}

interface PerformanceTreeRow extends PerformanceRow {
  hierarchy: string[];
  id: number;
}

interface ZScoreRow {
  key: 'pessimistic' | 'base' | 'optimistic';
  name: string;
  z: number;
}

interface VersionSession {
  method: ForecastMethod;
  zScores: ZScoreRow[];
  planReport: ProjectForecastReport | null;
  forecastReport: ProjectForecastReport | null;
  performanceRows: PerformanceRow[];
  planLoaded: boolean;
  forecastLoaded: boolean;
  factLoaded: boolean;
}

const DEFAULT_Z_SCORES: ZScoreRow[] = [
  { key: 'pessimistic', name: 'Пессимистичный', z: 0.5 },
  { key: 'base', name: 'Базовый', z: 0 },
  { key: 'optimistic', name: 'Оптимистичный', z: -0.5 },
];

function createEmptySession(): VersionSession {
  return {
    method: 'regression',
    zScores: DEFAULT_Z_SCORES.map((row) => ({ ...row })),
    planReport: null,
    forecastReport: null,
    performanceRows: [],
    planLoaded: false,
    forecastLoaded: false,
    factLoaded: false,
  };
}

function buildVolumeTreeRows(volumes: ForecastIicVolumeRow[]): VolumeTreeRow[] {
  const byParent = volumes.reduce<Map<number | null, ForecastIicVolumeRow[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  const result: VolumeTreeRow[] = [];
  const walk = (parentId: number | null, path: string[]) => {
    for (const child of byParent.get(parentId) ?? []) {
      const hierarchy = [...path, child.name];
      result.push({ ...child, hierarchy, id: child.workDirectionId });
      walk(child.workDirectionId, hierarchy);
    }
  };
  walk(null, []);
  return result;
}

function buildPerformanceTreeRows(rows: PerformanceRow[]): PerformanceTreeRow[] {
  const byParent = rows.reduce<Map<number | null, PerformanceRow[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  const result: PerformanceTreeRow[] = [];
  const walk = (parentId: number | null, path: string[]) => {
    for (const child of byParent.get(parentId) ?? []) {
      const hierarchy = [...path, child.workDirectionName];
      result.push({ ...child, hierarchy, id: child.workDirectionId });
      walk(child.workDirectionId, hierarchy);
    }
  };
  walk(null, []);
  return result;
}

function buildForecastScenarioTree(report: ProjectForecastReport): VolumeTreeRow[] {
  const pessimistic = report.scenarios.find((s) => s.scenarioName === 'Пессимистичный');
  const base = report.scenarios.find((s) => s.scenarioName === 'Базовый');
  const optimistic = report.scenarios.find((s) => s.scenarioName === 'Оптимистичный');

  const byId = new Map<number, VolumeTreeRow>();
  for (const row of buildVolumeTreeRows(report.plannedVolumes)) {
    const pes = pessimistic?.disciplines.find((d) => d.workDirectionId === row.workDirectionId);
    const bas = base?.disciplines.find((d) => d.workDirectionId === row.workDirectionId);
    const opt = optimistic?.disciplines.find((d) => d.workDirectionId === row.workDirectionId);
    byId.set(row.workDirectionId, {
      ...row,
      plannedVolume: row.plannedVolume,
      baseHours: bas?.baseHours ?? row.baseHours,
      optimisticHours: opt?.optimisticHours ?? row.optimisticHours,
      pessimisticHours: pes?.pessimisticHours ?? row.pessimisticHours,
      range: bas?.range ?? row.range,
    });
  }
  return [...byId.values()];
}

function directionLabel(direction: number): string {
  if (direction === ANALYSIS_PLAN) return 'План';
  if (direction === ANALYSIS_FACT) return 'Факт';
  if (direction === ANALYSIS_FORECAST) return 'Прогноз';
  return 'Прогноз';
}

function toCalculationOptions(method: ForecastMethod, zScores: ZScoreRow[]): ForecastCalculationOptions {
  const byKey = Object.fromEntries(zScores.map((row) => [row.key, row.z])) as Record<string, number>;
  return {
    method,
    zOptimistic: byKey.optimistic ?? -0.5,
    zBase: byKey.base ?? 0,
    zPessimistic: byKey.pessimistic ?? 0.5,
  };
}

export function ForecastPage({ projectId }: ForecastPageProps) {
  const { canRead } = usePermission('Planning');
  const { canEdit: canEditVersions } = usePermission('ProjectAnalysisVersions');
  const { canEdit: canImport } = usePermission('ProjectDataUpload');

  const [versions, setVersions] = useState<ProjectVersion[]>([]);
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [dataTab, setDataTab] = useState<DataViewTab>('forecast');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sessionsRef = useRef<Map<number, VersionSession>>(new Map());
  const [sessionTick, setSessionTick] = useState(0);

  const [versionModalOpen, setVersionModalOpen] = useState(false);
  const [editingVersion, setEditingVersion] = useState<ProjectVersion | null>(null);
  const [versionName, setVersionName] = useState('');
  const [versionDirection, setVersionDirection] = useState(ANALYSIS_PLAN);

  const ppvInputRef = useRef<HTMLInputElement>(null);
  const peqInputRef = useRef<HTMLInputElement>(null);

  const bumpSession = useCallback(() => setSessionTick((n) => n + 1), []);

  const getSession = useCallback((versionId: number): VersionSession => {
    let session = sessionsRef.current.get(versionId);
    if (!session) {
      session = createEmptySession();
      sessionsRef.current.set(versionId, session);
    }
    return session;
  }, []);

  const updateSession = useCallback(
    (versionId: number, patch: Partial<VersionSession>) => {
      const session = getSession(versionId);
      sessionsRef.current.set(versionId, { ...session, ...patch });
      bumpSession();
    },
    [bumpSession, getSession],
  );

  const session = selectedVersionId != null ? getSession(selectedVersionId) : createEmptySession();
  void sessionTick;

  const selectedVersion = useMemo(
    () => versions.find((v) => v.id === selectedVersionId) ?? null,
    [selectedVersionId, versions],
  );

  const loadVersions = useCallback(async () => {
    const list = await fetchProjectVersions(projectId);
    setVersions(list);
    if (!selectedVersionId && list.length > 0) {
      const plan = list.find((v) => v.direction === ANALYSIS_PLAN) ?? list[0];
      setSelectedVersionId(plan.id);
    }
  }, [projectId, selectedVersionId]);

  const loadPlanData = useCallback(
    async (versionId: number, force = false) => {
      const current = getSession(versionId);
      if (current.planLoaded && !force) return;
      const data = await fetchForecast(projectId, versionId, toCalculationOptions(current.method, current.zScores));
      updateSession(versionId, { planReport: data, planLoaded: true });
    },
    [getSession, projectId, updateSession],
  );

  const loadFactData = useCallback(
    async (versionId: number, force = false) => {
      const current = getSession(versionId);
      if (current.factLoaded && !force) return;
      const data = await fetchPerformance(projectId, versionId);
      updateSession(versionId, { performanceRows: data, factLoaded: true });
    },
    [getSession, projectId, updateSession],
  );

  const ensureTabData = useCallback(
    async (tab: DataViewTab, versionId: number, force = false) => {
      setLoading(true);
      setError(null);
      try {
        if (tab === 'fact') {
          await loadFactData(versionId, force);
        } else if (tab === 'plan') {
          await loadPlanData(versionId, force);
        }
      } catch {
        setError('Не удалось загрузить данные прогнозирования.');
      } finally {
        setLoading(false);
      }
    },
    [loadFactData, loadPlanData],
  );

  useEffect(() => {
    if (!canRead) return;
    void loadVersions().catch(() => setError('Не удалось загрузить версии.'));
  }, [canRead, loadVersions]);

  useEffect(() => {
    if (!selectedVersionId || !canRead) return;
    void ensureTabData(dataTab, selectedVersionId);
  }, [canRead, dataTab, ensureTabData, selectedVersionId]);

  const planTreeRows = useMemo(
    () => (session.planReport ? buildVolumeTreeRows(session.planReport.plannedVolumes) : []),
    [session.planReport],
  );

  const forecastTreeRows = useMemo(
    () => (session.forecastReport ? buildForecastScenarioTree(session.forecastReport) : []),
    [session.forecastReport],
  );

  const factTreeRows = useMemo(
    () => buildPerformanceTreeRows(session.performanceRows),
    [session.performanceRows],
  );

  const volumeColumnDefs = useMemo<ColDef<VolumeTreeRow>[]>(
    () => [
      { field: 'unitName', headerName: 'Ед.', maxWidth: 80 },
      { field: 'plannedVolume', headerName: 'План. объём', maxWidth: 120 },
      { field: 'baseHours', headerName: 'Базовые ч.', maxWidth: 110 },
      { field: 'optimisticHours', headerName: 'Опт.', maxWidth: 90 },
      { field: 'pessimisticHours', headerName: 'Пес.', maxWidth: 90 },
      { field: 'range', headerName: 'Диапазон', minWidth: 120 },
    ],
    [],
  );

  const factColumnDefs = useMemo<ColDef<PerformanceTreeRow>[]>(
    () => [
      { field: 'normUnitName', headerName: 'Ед.', maxWidth: 80 },
      { field: 'factPhysicalVolume', headerName: 'Факт. объём', maxWidth: 110 },
      { field: 'factLaborHours', headerName: 'Факт часы', maxWidth: 110 },
      { field: 'factProductivity', headerName: 'Производительность', maxWidth: 130 },
      { field: 'zScore', headerName: 'Z', maxWidth: 80 },
      { field: 'interpretation', headerName: 'Интерпретация', minWidth: 160 },
    ],
    [],
  );

  const zScoreDisplayRows = useMemo(() => {
    const report = session.forecastReport ?? session.planReport;
    if (report?.scenarios?.length) {
      return report.scenarios.map((s) => ({
        key: s.scenarioName,
        name: s.scenarioName,
        z: s.zScore,
        hours: s.totalLaborHours,
      }));
    }
    return session.zScores.map((row) => ({
      key: row.key,
      name: row.name,
      z: row.z,
      hours: null as number | null,
    }));
  }, [session.forecastReport, session.planReport, session.zScores]);

  const handleCalculate = async () => {
    if (!selectedVersionId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await calculateForecast(projectId, selectedVersionId, {
        calculation: toCalculationOptions(session.method, session.zScores),
      });
      updateSession(selectedVersionId, { forecastReport: data, forecastLoaded: true });
      setDataTab('forecast');
    } catch {
      setError('Не удалось выполнить расчёт прогноза.');
    } finally {
      setLoading(false);
    }
  };

  const handleZChange = (key: ZScoreRow['key'], value: string) => {
    if (!selectedVersionId) return;
    const next = session.zScores.map((row) =>
      row.key === key ? { ...row, z: value === '' ? 0 : Number(value) } : row,
    );
    updateSession(selectedVersionId, { zScores: next });
  };

  const handleMethodChange = (next: ForecastMethod) => {
    if (!selectedVersionId) return;
    updateSession(selectedVersionId, { method: next });
  };

  const openCreateVersion = () => {
    setEditingVersion(null);
    setVersionName('');
    setVersionDirection(ANALYSIS_PLAN);
    setVersionModalOpen(true);
  };

  const openEditVersion = () => {
    if (!selectedVersion) return;
    setEditingVersion(selectedVersion);
    setVersionName(selectedVersion.versionName);
    setVersionDirection(selectedVersion.direction);
    setVersionModalOpen(true);
  };

  const handleSaveVersion = async () => {
    if (!versionName.trim()) return;
    if (editingVersion) {
      await updateProjectVersion(projectId, editingVersion.id, {
        versionName: versionName.trim(),
        direction: versionDirection,
        name: versionName.trim(),
      });
    } else {
      const created = await createProjectVersion(projectId, {
        versionName: versionName.trim(),
        direction: versionDirection,
        name: versionName.trim(),
      });
      setSelectedVersionId(created.id);
    }
    setVersionModalOpen(false);
    await loadVersions();
  };

  const handleCopyVersion = async () => {
    if (!selectedVersionId) return;
    const copied = await copyProjectVersion(projectId, selectedVersionId);
    setSelectedVersionId(copied.id);
    await loadVersions();
  };

  const handleDeleteVersion = async () => {
    if (!selectedVersion || !window.confirm(`Удалить версию «${selectedVersion.versionName}»?`)) return;
    await deleteProjectVersion(projectId, selectedVersion.id);
    sessionsRef.current.delete(selectedVersion.id);
    setSelectedVersionId(null);
    await loadVersions();
  };

  const handleImport = async (dataType: 'physical-volumes' | 'equipment', file: File) => {
    if (!selectedVersionId) return;
    setLoading(true);
    try {
      await importProjectData(projectId, selectedVersionId, dataType, file);
      updateSession(selectedVersionId, {
        planLoaded: false,
        forecastLoaded: false,
        factLoaded: false,
        planReport: null,
        forecastReport: null,
        performanceRows: [],
      });
      await ensureTabData(dataTab, selectedVersionId, true);
    } catch {
      setError('Не удалось загрузить данные из файла.');
    } finally {
      setLoading(false);
    }
  };

  if (!canRead) {
    return <Alert severity="warning">Нет прав на прогнозирование.</Alert>;
  }

  const activeVolumeRows = dataTab === 'plan' ? planTreeRows : forecastTreeRows;
  const showForecastPlaceholder = dataTab === 'forecast' && !session.forecastLoaded;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%', minHeight: 0 }}>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="flex-start">
        <FormControl size="small" sx={{ minWidth: 280 }}>
          <InputLabel>Версия анализа</InputLabel>
          <Select
            label="Версия анализа"
            value={selectedVersionId ?? ''}
            onChange={(e) => setSelectedVersionId(Number(e.target.value))}
          >
            {versions.map((v) => (
              <MenuItem key={v.id} value={v.id}>
                {directionLabel(v.direction)} — {v.versionName}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {canEditVersions && (
          <Stack direction="row" spacing={1} flexWrap="wrap">
            <Button size="small" variant="outlined" onClick={openCreateVersion}>
              Создать
            </Button>
            <Button size="small" variant="outlined" onClick={openEditVersion} disabled={!selectedVersion}>
              Изменить
            </Button>
            <Button size="small" variant="outlined" onClick={() => void handleCopyVersion()} disabled={!selectedVersionId}>
              Копировать
            </Button>
            <Button size="small" variant="outlined" color="error" onClick={() => void handleDeleteVersion()} disabled={!selectedVersion}>
              Удалить
            </Button>
          </Stack>
        )}
      </Stack>

      {canImport && selectedVersionId && (
        <Stack direction="row" spacing={1} flexWrap="wrap">
          <input
            ref={ppvInputRef}
            type="file"
            accept=".xlsx,.xls"
            hidden
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) void handleImport('physical-volumes', file);
              e.target.value = '';
            }}
          />
          <input
            ref={peqInputRef}
            type="file"
            accept=".xlsx,.xls"
            hidden
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) void handleImport('equipment', file);
              e.target.value = '';
            }}
          />
          <Button size="small" variant="contained" onClick={() => ppvInputRef.current?.click()}>
            Загрузить физобъёмы
          </Button>
          <Button size="small" variant="contained" onClick={() => peqInputRef.current?.click()}>
            Загрузить оборудование
          </Button>
        </Stack>
      )}

      <Box>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Метод расчёта
        </Typography>
        <RadioGroup
          row
          value={session.method}
          onChange={(e) => handleMethodChange(e.target.value as ForecastMethod)}
        >
          <FormControlLabel value="regression" control={<Radio size="small" />} label="Регрессивный" />
          <FormControlLabel value="element" control={<Radio size="small" />} label="Поэлементный" />
        </RadioGroup>
      </Box>

      <Stack direction="row" spacing={2} alignItems="flex-start" flexWrap="wrap">
        <Table size="small" sx={{ maxWidth: 520 }}>
          <TableHead>
            <TableRow>
              <TableCell>Сценарий</TableCell>
              <TableCell>Z-оценка</TableCell>
              <TableCell>Трудозатраты, ч</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {session.zScores.map((row) => (
              <TableRow key={row.key}>
                <TableCell>{row.name}</TableCell>
                <TableCell>
                  <TextField
                    size="small"
                    type="number"
                    inputProps={{ step: 0.1 }}
                    value={row.z}
                    onChange={(e) => handleZChange(row.key, e.target.value)}
                    sx={{ width: 100 }}
                    aria-label={`Z-оценка ${row.name}`}
                  />
                </TableCell>
                <TableCell>
                  {zScoreDisplayRows.find((r) => r.key === row.key || r.name === row.name)?.hours != null
                    ? Number(
                        zScoreDisplayRows.find((r) => r.key === row.key || r.name === row.name)!.hours,
                      ).toFixed(0)
                    : '—'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <Button variant="contained" onClick={() => void handleCalculate()} disabled={!selectedVersionId || loading}>
          Рассчитать
        </Button>
      </Stack>

      <Tabs value={dataTab} onChange={(_, v: DataViewTab) => setDataTab(v)}>
        <Tab value="plan" label="План" />
        <Tab value="fact" label="Факт" />
        <Tab value="forecast" label="Прогноз" />
      </Tabs>

      {error && <Alert severity="error">{error}</Alert>}

      {!selectedVersionId ? (
        <Alert severity="info">Выберите или создайте версию анализа.</Alert>
      ) : showForecastPlaceholder ? (
        <Alert severity="info">Нажмите «Рассчитать» для получения данных вкладки «Прогноз».</Alert>
      ) : (
        <Box sx={{ flex: 1, minHeight: 360 }}>
          {dataTab === 'fact' ? (
            <DictionaryGrid<PerformanceTreeRow>
              rowData={factTreeRows}
              columnDefs={factColumnDefs}
              loading={loading}
              objectName="Planning"
              gridId={`forecast-${projectId}-fact`}
              treeData
              hierarchyField="hierarchy"
              autoGroupColumnDef={{
                headerName: 'Группа IIC',
                minWidth: 260,
                field: 'workDirectionName',
                cellRendererParams: { suppressCount: true },
              }}
            />
          ) : (
            <DictionaryGrid<VolumeTreeRow>
              rowData={activeVolumeRows}
              columnDefs={volumeColumnDefs}
              loading={loading}
              objectName="Planning"
              gridId={`forecast-${projectId}-${dataTab}`}
              treeData
              hierarchyField="hierarchy"
              autoGroupColumnDef={{
                headerName: 'Группа IIC',
                minWidth: 260,
                field: 'name',
                cellRendererParams: { suppressCount: true },
              }}
            />
          )}
        </Box>
      )}

      {versionModalOpen && (
        <Box
          sx={{
            position: 'fixed',
            inset: 0,
            bgcolor: 'rgba(0,0,0,0.4)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1300,
          }}
          onClick={() => setVersionModalOpen(false)}
        >
          <Box
            sx={{ bgcolor: 'background.paper', p: 3, borderRadius: 2, minWidth: 360 }}
            onClick={(e) => e.stopPropagation()}
          >
            <Typography variant="h6" sx={{ mb: 2 }}>
              {editingVersion ? 'Редактирование версии' : 'Новая версия'}
            </Typography>
            <Stack spacing={2}>
              <TextField
                label="Название версии"
                value={versionName}
                onChange={(e) => setVersionName(e.target.value)}
                fullWidth
                size="small"
                required
              />
              <FormControl fullWidth size="small">
                <InputLabel>Тип</InputLabel>
                <Select
                  label="Тип"
                  value={versionDirection}
                  onChange={(e) => setVersionDirection(Number(e.target.value))}
                  data-testid="forecast-version-type-select"
                >
                  <MenuItem value={ANALYSIS_PLAN}>План</MenuItem>
                  <MenuItem value={ANALYSIS_FACT}>Факт</MenuItem>
                  <MenuItem value={ANALYSIS_FORECAST}>Прогноз</MenuItem>
                </Select>
              </FormControl>
              <Stack direction="row" spacing={1} justifyContent="flex-end">
                <Button onClick={() => setVersionModalOpen(false)}>Отмена</Button>
                <Button variant="contained" onClick={() => void handleSaveVersion()}>
                  Сохранить
                </Button>
              </Stack>
            </Stack>
          </Box>
        </Box>
      )}
    </Box>
  );
}
