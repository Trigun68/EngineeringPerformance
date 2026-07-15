import { useCallback, useEffect, useMemo, useState } from 'react';
import TableViewIcon from '@mui/icons-material/TableView';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import {
  Alert,
  Box,
  Button,
  Stack,
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
  exportForecastExcel,
  exportForecastPdf,
  fetchForecast,
  previewForecast,
  saveForecastPlannedVolumes,
  type ForecastIicVolumeRow,
  type ProjectForecastReport,
} from '../../api/analyticsApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { usePermission } from '../../hooks/usePermission';
import { collectNormFallbackWarnings } from '../../features/projects/forecast/normFallbackWarning';

interface PlanningTabProps {
  projectId: number;
  versionId: number | null;
}

interface VolumeTreeRow extends ForecastIicVolumeRow {
  hierarchy: string[];
  isLeaf: boolean;
}

function buildTreeRows(volumes: ForecastIicVolumeRow[]): VolumeTreeRow[] {
  const idsWithChildren = new Set(
    volumes.filter((v) => v.parentId != null).map((v) => v.parentId!),
  );

  const byParent = volumes.reduce<Map<number | null, ForecastIicVolumeRow[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  const result: VolumeTreeRow[] = [];
  const walk = (parentId: number | null, path: string[]) => {
    const children = byParent.get(parentId) ?? [];
    for (const child of children) {
      const hierarchy = [...path, child.name];
      result.push({
        ...child,
        hierarchy,
        isLeaf: !idsWithChildren.has(child.workDirectionId),
      });
      walk(child.workDirectionId, hierarchy);
    }
  };
  walk(null, []);
  return result;
}

export function PlanningTab({ projectId, versionId }: PlanningTabProps) {
  const { canRead, canEdit } = usePermission('Planning');
  const [report, setReport] = useState<ProjectForecastReport | null>(null);
  const [volumeEdits, setVolumeEdits] = useState<Record<number, string>>({});
  const [dirtyIds, setDirtyIds] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!versionId || !canRead) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchForecast(projectId, versionId);
      setReport(data);
      setVolumeEdits({});
      setDirtyIds(new Set());
    } catch {
      setError('Не удалось загрузить прогноз.');
    } finally {
      setLoading(false);
    }
  }, [projectId, versionId, canRead]);

  useEffect(() => {
    void load();
  }, [load]);

  const treeRows = useMemo(
    () => (report ? buildTreeRows(report.plannedVolumes) : []),
    [report],
  );

  const leafRows = useMemo(() => treeRows.filter((r) => r.isLeaf), [treeRows]);

  const normFallbackWarnings = useMemo(
    () =>
      collectNormFallbackWarnings(
        leafRows.map((r) => ({
          workDirectionId: r.workDirectionId,
          name: r.name,
          plannedVolume: r.plannedVolume,
          normProductivity: r.normProductivity,
          baseHours: r.baseHours,
        })),
      ),
    [leafRows],
  );

  const runPreview = async (edits: Record<number, string>) => {
    if (!versionId) return;
    const nodePlannedTotals: Record<number, number> = {};
    for (const row of treeRows) {
      const raw = edits[row.workDirectionId] ?? String(row.plannedVolume);
      nodePlannedTotals[row.workDirectionId] = Number(raw) || 0;
    }
    const preview = await previewForecast(projectId, versionId, nodePlannedTotals);
    setReport((prev) =>
      prev
        ? {
            ...prev,
            scenarios: preview.scenarios,
            interpretations: preview.interpretations,
            top3Disciplines: preview.top3Disciplines,
          }
        : preview,
    );
  };

  const handleVolumeChange = (row: VolumeTreeRow, value: string) => {
    const nextEdits = { ...volumeEdits, [row.workDirectionId]: value };
    const nextDirty = new Set(dirtyIds);
    nextDirty.add(row.workDirectionId);
    setVolumeEdits(nextEdits);
    setDirtyIds(nextDirty);
    void runPreview(nextEdits).catch(() => undefined);
  };

  const handleSave = async () => {
    if (!versionId || dirtyIds.size === 0) return;
    setLoading(true);
    setError(null);
    try {
      const items = [...dirtyIds].map((id) => ({
        workDirectionId: id,
        quantity: Number(volumeEdits[id] ?? leafRows.find((r) => r.workDirectionId === id)?.plannedVolume ?? 0),
      }));
      await saveForecastPlannedVolumes(projectId, versionId, items);
      await load();
    } catch {
      setError('Не удалось сохранить плановые объёмы.');
      setLoading(false);
    }
  };

  const columnDefs = useMemo<ColDef<VolumeTreeRow>[]>(
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

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр прогноза.</Alert>;
  }

  if (!versionId) {
    return <Alert severity="info">Выберите версию анализа.</Alert>;
  }

  return (
    <Box>
      {versionId && (
        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<TableViewIcon />}
            onClick={() => void exportForecastExcel(projectId, versionId)}
          >
            Excel
          </Button>
          <Button
            size="small"
            variant="outlined"
            startIcon={<PictureAsPdfIcon />}
            onClick={() => void exportForecastPdf(projectId, versionId)}
          >
            PDF
          </Button>
        </Stack>
      )}
      {normFallbackWarnings.length > 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {normFallbackWarnings.map((w) => (
            <div key={w}>{w}</div>
          ))}
        </Alert>
      )}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {canEdit && dirtyIds.size > 0 && (
        <Button variant="contained" sx={{ mb: 2 }} onClick={() => void handleSave()} disabled={loading}>
          Сохранить плановые объёмы ({dirtyIds.size})
        </Button>
      )}

      <DataTable<VolumeTreeRow>
        rowData={treeRows}
        columnDefs={columnDefs}
        loading={loading}
        objectName="Planning"
        gridId={`project-${projectId}-planning-volumes`}
        treeData
        getDataPath={(row) => row.hierarchy}
        autoGroupColumnDef={{ headerName: 'Группа IIC', minWidth: 260, field: 'name' }}
      />

      {canEdit && leafRows.length > 0 && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>Редактирование плановых объёмов (листья)</Typography>
          <Stack spacing={1}>
            {leafRows.slice(0, 20).map((row) => (
              <Stack key={row.workDirectionId} direction="row" spacing={1} alignItems="center">
                <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }} noWrap title={row.hierarchyPath}>
                  {row.hierarchyPath}
                </Typography>
                <TextField
                  size="small"
                  type="number"
                  label="Объём"
                  value={volumeEdits[row.workDirectionId] ?? String(row.plannedVolume)}
                  onChange={(e) => handleVolumeChange(row, e.target.value)}
                  sx={{ width: 140 }}
                />
              </Stack>
            ))}
            {leafRows.length > 20 && (
              <Typography variant="caption" color="text.secondary">
                Показаны первые 20 листьев из {leafRows.length}
              </Typography>
            )}
          </Stack>
        </Box>
      )}

      {report && (
        <Stack spacing={2} sx={{ mt: 3 }}>
          <Typography variant="subtitle1">Сценарии</Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Сценарий</TableCell>
                <TableCell>Z</TableCell>
                <TableCell>Часы</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {report.scenarios.map((s) => (
                <TableRow key={s.scenarioName}>
                  <TableCell>{s.scenarioName}</TableCell>
                  <TableCell>{s.zScore}</TableCell>
                  <TableCell>{s.totalLaborHours.toFixed(2)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          <Typography variant="subtitle1">Интерпретация</Typography>
          <Table size="small">
            <TableBody>
              {report.interpretations.map((i) => (
                <TableRow key={i.scenarioName}>
                  <TableCell>{i.scenarioName}</TableCell>
                  <TableCell>{i.laborHours.toFixed(2)} ч ({i.percentOfBase}%)</TableCell>
                  <TableCell>{i.interpretation}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {report.top3Disciplines.length > 0 && (
            <>
              <Typography variant="subtitle1">ТОП-3 дисциплины</Typography>
              <ul>
                {report.top3Disciplines.map((d) => (
                  <li key={d.rank}>
                    {d.rank}. {d.disciplineName} — {d.baseHours.toFixed(2)} ч
                  </li>
                ))}
              </ul>
            </>
          )}
        </Stack>
      )}
    </Box>
  );
}
