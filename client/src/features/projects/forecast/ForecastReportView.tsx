import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
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
import RefreshIcon from '@mui/icons-material/Refresh';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import {
  exportForecastSummaryExcel,
  exportForecastSummaryPdf,
} from '../../../api/projectsPageApi';
import { fetchForecastAssessmentVolumes, type ProjectVersionPhysicalVolume } from '../../../api/analyticsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { useDictionaryStore } from '../../../store/useDictionaryStore';
import { formatGridNumber } from '../../../utils/formatGridNumber';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { REPORT_COLUMN_HEADER_FONT_SIZE } from '../utils/projectModalTitle';
import { ReportHeaderCell } from '../utils/ReportHeaderCell';
import type { ProjectsPageForecastSummaryRow } from '../types';
import { sumVolumeTotals, volumesForDirection } from './forecastReportSubtree';

export interface ForecastReportViewProps {
  projectId: number;
  versionId: number;
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
        color: '#FFFFFF',
        px: 2,
        py: 1.5,
        borderRadius: 1,
        mb: 0.5,
        fontSize: REPORT_COLUMN_HEADER_FONT_SIZE,
        fontWeight: 600,
        alignItems: 'end',
      }}
    >
      <ReportHeaderCell>Шифр</ReportHeaderCell>
      <ReportHeaderCell>Направление</ReportHeaderCell>
      <ReportHeaderCell align="right">Факт ФО</ReportHeaderCell>
      <ReportHeaderCell align="right">Норм. произв.</ReportHeaderCell>
      <ReportHeaderCell align="right" sx={{ color: '#27AE60' }}>
        Оптимист., ч
      </ReportHeaderCell>
      <ReportHeaderCell align="right">Базовый, ч</ReportHeaderCell>
      <ReportHeaderCell align="right" sx={{ color: '#E74C3C' }}>
        Пессимист., ч
      </ReportHeaderCell>
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
        py: 1.25,
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
      <Box sx={{ textAlign: 'right', fontWeight: 600, color: dark ? '#A9DFBF' : '#27AE60' }}>
        {num1(optH)}
      </Box>
      <Box sx={{ textAlign: 'right', fontWeight: 600 }}>{num1(baseH)}</Box>
      <Box sx={{ textAlign: 'right', fontWeight: 600, color: dark ? '#F1948A' : '#E74C3C' }}>
        {num1(pesH)}
      </Box>
    </Box>
  );
}

export function ForecastReportView({
  projectId,
  versionId,
  summaryRows,
}: ForecastReportViewProps) {
  const getById = useDictionaryStore((s) => s.getById);
  const getUnitName = useDictionaryStore((s) => s.getUnitName);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pvByDirection, setPvByDirection] = useState<Record<string, ProjectVersionPhysicalVolume[]>>({});
  const [eqByDirection, setEqByDirection] = useState<Record<string, ProjectVersionPhysicalVolume[]>>({});
  const [exporting, setExporting] = useState(false);

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
    setError(null);
    try {
      if (type === 'excel') await exportForecastSummaryExcel(projectId, versionId);
      else await exportForecastSummaryPdf(projectId, versionId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт прогноза.'));
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
      <Stack direction="row" spacing={1} justifyContent="flex-end" alignItems="center" sx={{ mb: 2, flexShrink: 0 }}>
        <Stack direction="row" spacing={1}>
          <ExportActionButton
            kind="excel"
            loading={exporting}
            onClick={() => void handleExport('excel')}
          />
          <ExportActionButton
            kind="pdf"
            loading={exporting}
            onClick={() => void handleExport('pdf')}
          />
          <ToolbarActionButton
            startIcon={<RefreshIcon />}
            onClick={() => void loadVolumes()}
            disabled={loading}
          >
            {ACTION_LABELS.refresh}
          </ToolbarActionButton>
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
            const pv = volumesForDirection(pvByDirection, row.workDirectionId);
            const eq = volumesForDirection(eqByDirection, row.workDirectionId);
            const volumeRowCount = pv.length + eq.length;
            const factFo =
              volumeRowCount > 0
                ? sumVolumeTotals(pv) + sumVolumeTotals(eq)
                : row.factPhysicalVolume;
            return (
              <Paper
                key={`${row.workDirectionId}-${name}`}
                data-testid={`forecast-report-card-${row.workDirectionId}`}
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
                  factFo={factFo}
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
