import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Grid,
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
import {
  exportForecastExcel,
  exportForecastPdf,
  fetchForecastAssessmentVolumes,
  type ProjectVersionPhysicalVolume,
} from '../../../api/analyticsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import { formatGridNumber } from '../../../utils/formatGridNumber';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import type { ProjectsPageForecastSummaryRow } from '../types';

export interface ForecastReportViewProps {
  projectId: number;
  versionId: number;
  projectName: string;
  projectCode: string;
  versionLabel: string;
  summaryRows: ProjectsPageForecastSummaryRow[];
}

const num1 = (v: unknown) => formatGridNumber(v, 1);
const num2 = (v: unknown) => formatGridNumber(v, 2);
const num4 = (v: unknown) => formatGridNumber(v, 4);

const GRID_COLUMNS = '80px 1fr 120px 110px 110px 110px 110px';

function VolumeTable({ title, rows }: { title: string; rows: ProjectVersionPhysicalVolume[] }) {
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
      <Box sx={{ textAlign: 'right' }}>Норм. произв.</Box>
      <Box sx={{ textAlign: 'right' }}>Базовый, ч</Box>
      <Box sx={{ textAlign: 'right', color: '#27AE60' }}>Оптимист., ч</Box>
      <Box sx={{ textAlign: 'right', color: '#E74C3C' }}>Пессимист., ч</Box>
    </Box>
  );
}

function ReportDataRow({
  code,
  name,
  factFo,
  normProd,
  baseH,
  optH,
  pesH,
  level = 0,
  dark = false,
}: {
  code: string;
  name: string;
  factFo: unknown;
  normProd: unknown;
  baseH: unknown;
  optH: unknown;
  pesH: unknown;
  level?: number;
  dark?: boolean;
}) {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: GRID_COLUMNS,
        gap: 1,
        alignItems: 'center',
        px: 2,
        py: dark ? 1 : 1.25,
        ml: `${level * 24}px`,
        bgcolor: dark ? '#1E3A5F' : '#FFFFFF',
        color: dark ? '#FFFFFF' : '#2C3E50',
        border: dark ? 'none' : '1px solid #E2E8F0',
        borderRadius: 1,
        mb: dark ? 1 : 0.5,
        fontSize: dark ? 13 : 14,
      }}
    >
      <Box sx={{ fontWeight: dark ? 700 : 600, fontFamily: code ? 'monospace' : 'inherit' }}>
        {code || (dark ? 'ИТОГО' : '—')}
      </Box>
      <Box sx={{ fontWeight: dark ? 600 : 500 }}>{name}</Box>
      <Box sx={{ textAlign: 'right' }}>
        {typeof factFo === 'string'
          ? factFo
          : dark
            ? (factFo != null && factFo !== '' ? String(factFo) : '—')
            : num2(factFo)}
      </Box>
      <Box sx={{ textAlign: 'right' }}>{dark ? '—' : num4(normProd)}</Box>
      <Box sx={{ textAlign: 'right', fontWeight: 600 }}>{num1(baseH)}</Box>
      <Box sx={{ textAlign: 'right', fontWeight: 600, color: dark ? '#A9DFBF' : '#27AE60' }}>
        {num1(optH)}
      </Box>
      <Box sx={{ textAlign: 'right', fontWeight: 600, color: dark ? '#F1948A' : '#E74C3C' }}>
        {num1(pesH)}
      </Box>
    </Box>
  );
}

export function ForecastReportView({
  projectId,
  versionId,
  projectName,
  projectCode,
  versionLabel,
  summaryRows,
}: ForecastReportViewProps) {
  const getById = useDictionaryStore((s) => s.getById);
  const getUnitName = useDictionaryStore((s) => s.getUnitName);
  const fetchAll = useDictionaryStore((s) => s.fetchAll);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pvByDirection, setPvByDirection] = useState<Record<string, ProjectVersionPhysicalVolume[]>>({});
  const [eqByDirection, setEqByDirection] = useState<Record<string, ProjectVersionPhysicalVolume[]>>({});
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    void fetchAll();
  }, [fetchAll]);

  const loadVolumes = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchForecastAssessmentVolumes(projectId, versionId);
      setPvByDirection(data.physicalVolumesByDirection ?? {});
      setEqByDirection(data.equipmentVolumesByDirection ?? {});
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить физ. объёмы для отчёта.'));
    } finally {
      setLoading(false);
    }
  }, [projectId, versionId]);

  useEffect(() => {
    void loadVolumes();
  }, [loadVolumes]);

  const totalRow = useMemo(
    () => summaryRows.find((r) => r.isTotal && r.level === -1),
    [summaryRows],
  );

  const cardRows = useMemo(
    () => summaryRows.filter((r) => !(r.isTotal && r.level === -1) && !r.isTotal),
    [summaryRows],
  );

  const handleExport = async (type: 'excel' | 'pdf') => {
    setExporting(true);
    try {
      if (type === 'excel') await exportForecastExcel(projectId, versionId);
      else await exportForecastPdf(projectId, versionId);
    } finally {
      setExporting(false);
    }
  };

  const resolveDirectionMeta = (row: ProjectsPageForecastSummaryRow) => {
    const item = getById('iicWorkDirections', row.workDirectionId);
    const rawName = row.displayName.trim().replace(/^Итого:\s*/i, '');
    const unitName = row.normUnitName || (item?.unitId ? getUnitName(item.unitId) : '');
    if (item) {
      return {
        code: item.code?.trim() ?? row.code?.trim() ?? '',
        name: formatIisNorm({
          code: item.code,
          name: item.name,
          unitName,
        }),
      };
    }
    return {
      code: row.code?.trim() ?? '',
      name: formatIisNorm({
        code: row.code,
        name: rawName,
        unitName: row.normUnitName,
      }),
    };
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0 }}>
      <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center" sx={{ mb: 2, flexShrink: 0 }}>
        <Box>
          <Typography variant="body2" color="text.secondary">
            {projectCode} — {projectName} ({versionLabel})
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
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
          <Button size="small" variant="outlined" onClick={() => void loadVolumes()} disabled={loading}>
            Обновить
          </Button>
        </Stack>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {loading && cardRows.length === 0 ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress size={28} />
        </Box>
      ) : (
        <Box sx={{ flex: 1, overflow: 'auto', pr: 0.5 }}>
          <ReportHeaderRow />

          {totalRow && (
            <ReportDataRow
              code=""
              name="По проекту"
              factFo={totalRow.factPhysicalVolumeDisplay ?? totalRow.factPhysicalVolume}
              normProd={null}
              baseH={totalRow.baseHours}
              optH={totalRow.optimisticHours}
              pesH={totalRow.pessimisticHours}
              dark
            />
          )}

          {cardRows.map((row) => {
            const { code, name } = resolveDirectionMeta(row);
            const pv = pvByDirection[String(row.workDirectionId)] ?? [];
            const eq = eqByDirection[String(row.workDirectionId)] ?? [];
            return (
              <Paper
                key={`${row.workDirectionId}-${name}`}
                variant="outlined"
                sx={{
                  mb: 1.5,
                  p: 1.5,
                  ml: `${Math.max(0, row.level) * 24}px`,
                  borderColor: '#E2E8F0',
                }}
              >
                <ReportDataRow
                  code={code}
                  name={name}
                  factFo={row.factPhysicalVolumeDisplay ?? row.factPhysicalVolume}
                  normProd={row.normProductivity}
                  baseH={row.baseHours}
                  optH={row.optimisticHours}
                  pesH={row.pessimisticHours}
                />
                <Grid container spacing={1} sx={{ mt: 0.5, px: 1 }}>
                  {row.normUnitName && (
                    <Grid item>
                      <Typography variant="caption" sx={{ color: '#64748B' }}>
                        ед. изм.: <strong>{row.normUnitName}</strong>
                      </Typography>
                    </Grid>
                  )}
                </Grid>
                <VolumeTable title="Физ. объёмы проекта" rows={pv} />
                <VolumeTable title="Оборудование проекта" rows={eq} />
              </Paper>
            );
          })}
        </Box>
      )}
    </Box>
  );
}
