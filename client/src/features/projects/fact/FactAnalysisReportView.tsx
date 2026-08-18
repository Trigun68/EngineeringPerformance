import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import RefreshIcon from '@mui/icons-material/Refresh';
import {
  exportAnalysisExcel,
  exportAnalysisPdf,
  fetchVersionAnalysis,
  type ProjectVersionAnalysis,
  type ProjectVersionDirection,
  type ProjectVersionLaborRow,
  type ProjectVersionPhysicalVolumeItem,
} from '../../../api/analyticsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { formatGridNumber } from '../../../utils/formatGridNumber';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { usePermission } from '../../../hooks/usePermission';

export interface FactAnalysisReportViewProps {
  projectId: number;
  versionId: number;
}

const num1 = (v: unknown) => formatGridNumber(v, 1);
const num2 = (v: unknown) => formatGridNumber(v, 2);
const num4 = (v: unknown) => formatGridNumber(v, 4);

const GRID_COLUMNS = '80px 1fr 100px 90px 110px 110px 110px 100px 100px 90px 140px';

function VolumeTable({ title, rows }: { title: string; rows: ProjectVersionPhysicalVolumeItem[] }) {
  if (rows.length === 0) return null;
  return (
    <Box sx={{ mt: 1.5 }}>
      <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600, display: 'block', mb: 0.5 }}>
        {title}
      </Typography>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Код</TableCell>
            <TableCell>Номенклатурная позиция</TableCell>
            <TableCell>Ед.</TableCell>
            <TableCell align="right">Кол-во</TableCell>
            <TableCell align="right">Коэфф.</TableCell>
            <TableCell align="right">Итого</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row, idx) => (
            <TableRow key={`${row.code}-${idx}`}>
              <TableCell>{row.code}</TableCell>
              <TableCell>{row.name}</TableCell>
              <TableCell>{row.unitName ?? '—'}</TableCell>
              <TableCell align="right">{num2(row.quantity)}</TableCell>
              <TableCell align="right">{num4(row.coefficient)}</TableCell>
              <TableCell align="right">{num2(row.total)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  );
}

function LaborBlock({ rows }: { rows: ProjectVersionLaborRow[] }) {
  const total = rows.reduce((s, r) => s + (r.plannedHours ?? 0), 0);
  return (
    <Box sx={{ mt: 3 }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1, color: '#2C3E50' }}>
        Трудоёмкость
      </Typography>
      <Typography variant="caption" sx={{ color: '#64748B', display: 'block', mb: 1 }}>
        Виды работ проекта (структура загруженной трудоёмкости)
      </Typography>
      {rows.length === 0 ? (
        <Alert severity="info">Трудоёмкость не загружена для этой версии.</Alert>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Шифр документации</TableCell>
              <TableCell>Марка</TableCell>
              <TableCell>Направление IIC</TableCell>
              <TableCell align="right">Плановые часы</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row, idx) => (
              <TableRow key={`${row.documentationCode}-${idx}`}>
                <TableCell>{row.documentationCode}</TableCell>
                <TableCell>{row.brand ?? '—'}</TableCell>
                <TableCell>{row.workDirectionName ?? '—'}</TableCell>
                <TableCell align="right">{num1(row.plannedHours)}</TableCell>
              </TableRow>
            ))}
            <TableRow sx={{ bgcolor: '#EEF4FF' }}>
              <TableCell colSpan={3} sx={{ fontWeight: 700 }}>
                ИТОГО
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {num1(total)}
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      )}
    </Box>
  );
}

function ReportHeaderRow() {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: GRID_COLUMNS,
        gap: 1,
        bgcolor: '#2C3E50',
        color: '#94A3B8',
        px: 2,
        py: 1,
        borderRadius: 1,
        mb: 0.5,
        fontSize: 11,
        fontWeight: 600,
      }}
    >
      <Box>Шифр</Box>
      <Box>Направление</Box>
      <Box sx={{ textAlign: 'right' }}>Факт ФО</Box>
      <Box sx={{ textAlign: 'right' }}>Факт ч</Box>
      <Box sx={{ textAlign: 'right' }}>План.Труд.Песс.</Box>
      <Box sx={{ textAlign: 'right' }}>План.Труд.Баз.</Box>
      <Box sx={{ textAlign: 'right' }}>План.Труд.Оптим.</Box>
      <Box sx={{ textAlign: 'right' }}>Факт произв.</Box>
      <Box sx={{ textAlign: 'right' }}>Норм. произв.</Box>
      <Box sx={{ textAlign: 'right' }}>Z</Box>
      <Box>Интерпретация</Box>
    </Box>
  );
}

function directionTitle(d: ProjectVersionDirection): string {
  return formatIisNorm({ code: d.code, name: d.name, unitName: d.unitName });
}

function interpretLabel(v: string | number | null | undefined): string {
  if (v == null) return '—';
  if (typeof v === 'number') return String(v);
  return v || '—';
}

export function FactAnalysisReportView({ projectId, versionId }: FactAnalysisReportViewProps) {
  const { canRead } = usePermission('Assessment');
  const [report, setReport] = useState<ProjectVersionAnalysis | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  const load = useCallback(async () => {
    if (!canRead) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchVersionAnalysis(projectId, versionId);
      setReport(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить отчёт анализа версии.'));
      setReport(null);
    } finally {
      setLoading(false);
    }
  }, [canRead, projectId, versionId]);

  useEffect(() => {
    void load();
  }, [load]);

  const directionsSorted = useMemo(() => {
    if (!report) return [];
    return [...report.directions].sort(
      (a, b) => a.level - b.level || a.code.localeCompare(b.code, 'ru'),
    );
  }, [report]);

  const handleExport = async (type: 'excel' | 'pdf') => {
    setExporting(true);
    try {
      if (type === 'excel') await exportAnalysisExcel(projectId, versionId);
      else await exportAnalysisPdf(projectId, versionId);
    } finally {
      setExporting(false);
    }
  };

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр оценки факта.</Alert>;
  }

  if (loading && !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress size={32} />
      </Box>
    );
  }

  if (error && !report) {
    return <Alert severity="error">{error}</Alert>;
  }

  if (!report) return null;

  const s = report.summary;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0 }}>
      <Stack
        direction="row"
        spacing={1}
        justifyContent="space-between"
        alignItems="center"
        sx={{ mb: 2, flexShrink: 0 }}
      >
        <Box>
          <Typography variant="body2" color="text.secondary">
            {report.projectCode ?? '—'} — {report.projectName} ({report.versionName})
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {[
              s.weightedZScore != null ? `Z (взвеш.): ${num4(s.weightedZScore)}` : null,
              s.weightedZScorePercentile ? `Перцентиль: ${s.weightedZScorePercentile}` : null,
              s.totalFactLaborHours != null ? `Часы: ${num1(s.totalFactLaborHours)}` : null,
            ]
              .filter(Boolean)
              .join(' · ')}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<RefreshIcon />}
            disabled={loading || exporting}
            onClick={() => void load()}
          >
            Обновить
          </Button>
          <Button
            size="small"
            variant="contained"
            sx={{ bgcolor: '#27AE60', '&:hover': { bgcolor: '#229954' } }}
            startIcon={<FileDownloadIcon />}
            disabled={exporting}
            onClick={() => void handleExport('excel')}
          >
            Excel
          </Button>
          <Button
            size="small"
            variant="contained"
            sx={{ bgcolor: '#E74C3C', '&:hover': { bgcolor: '#C0392B' } }}
            startIcon={<PictureAsPdfIcon />}
            disabled={exporting}
            onClick={() => void handleExport('pdf')}
          >
            PDF
          </Button>
        </Stack>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 1 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ flex: 1, overflow: 'auto', minHeight: 0 }}>
        <ReportHeaderRow />
        {directionsSorted.map((d) => {
          const pv = d.physicalVolumes ?? [];
          const eq = d.equipmentVolumes ?? [];
          const hasVolumes = pv.length > 0 || eq.length > 0;
          return (
            <Paper
              key={d.id}
              elevation={0}
              sx={{
                mb: 1,
                ml: `${d.level * 24}px`,
                border: '1px solid #E2E8F0',
                borderRadius: 1,
                overflow: 'hidden',
              }}
            >
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: GRID_COLUMNS,
                  gap: 1,
                  alignItems: 'center',
                  px: 2,
                  py: 1.25,
                  bgcolor: '#FFFFFF',
                  fontSize: 14,
                }}
              >
                <Box sx={{ fontWeight: 600, fontFamily: 'monospace' }}>{d.code || '—'}</Box>
                <Box sx={{ fontWeight: 500 }}>{directionTitle(d)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num2(d.factPhysicalVolume)}</Box>
                <Box sx={{ textAlign: 'right', fontWeight: 600 }}>{num1(d.factLaborHours)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num2(d.pessimisticHours)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num2(d.baseHours)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num2(d.optimisticHours)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num4(d.factProductivity)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num4(d.normProductivity)}</Box>
                <Box sx={{ textAlign: 'right' }}>{num4(d.zScore)}</Box>
                <Box>{interpretLabel(d.interpretation)}</Box>
              </Box>
              {hasVolumes && (
                <Box sx={{ px: 2, pb: 1.5, bgcolor: '#F8FAFC' }}>
                  <VolumeTable title="Физ. объёмы проекта" rows={pv} />
                  <VolumeTable title="Оборудование проекта" rows={eq} />
                </Box>
              )}
            </Paper>
          );
        })}

        <LaborBlock rows={report.laborIntensities ?? []} />
      </Box>
    </Box>
  );
}
