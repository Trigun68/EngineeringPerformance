import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Box,
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
import RefreshIcon from '@mui/icons-material/Refresh';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import {
  exportAnalysisExcel,
  exportAnalysisPdf,
  fetchVersionAnalysis,
  type ProjectVersionDirectionLaborRow,
  type ProjectVersionLaborRow,
  type ProjectVersionPhysicalVolumeItem,
} from '../../../api/analyticsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import { formatGridNumber } from '../../../utils/formatGridNumber';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { REPORT_COLUMN_HEADER_FONT_SIZE } from '../utils/projectModalTitle';
import { ReportHeaderCell } from '../utils/ReportHeaderCell';
import { usePermission } from '../../../hooks/usePermission';
import {
  factFoForCard,
  orderDirectionsForCards,
  ownLabor,
  ownVolumes,
  sumFactCardHours,
} from './factAnalysisTree';

export interface FactAnalysisReportViewProps {
  projectId: number;
  versionId: number;
}

const num1 = (v: unknown) => formatGridNumber(v, 1);
const num2 = (v: unknown) => formatGridNumber(v, 2);
const num4 = (v: unknown) => formatGridNumber(v, 4);

/** Пастель колонок плана — как грид «Оценка факта / Итого». */
const PLAN_HOUR_BG = {
  optimistic: '#e3f2fd',
  base: '#e8f5e9',
  pessimistic: '#ffebee',
} as const;

const OPT_COLOR = '#27AE60';
const PES_COLOR = '#E74C3C';

const GRID_COLUMNS = '90px minmax(220px, 1.4fr) 110px 100px 130px 120px 130px 110px 110px 80px minmax(140px, 1fr)';
const GRID_MIN_WIDTH = 1280;

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

function DirectionLaborTable({ rows }: { rows: ProjectVersionDirectionLaborRow[] }) {
  if (rows.length === 0) return null;
  const planned = rows.reduce((s, r) => s + (r.plannedHours ?? 0), 0);
  const actual = rows.reduce((s, r) => s + (r.actualHours ?? 0), 0);
  return (
    <Box sx={{ mt: 1.5 }}>
      <Typography variant="caption" sx={{ color: '#64748B', fontWeight: 600, display: 'block', mb: 0.5 }}>
        Трудоёмкость версии
      </Typography>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Шифр документации</TableCell>
            <TableCell>Марка</TableCell>
            <TableCell align="right">План, ч</TableCell>
            <TableCell align="right">Факт, ч</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row, idx) => (
            <TableRow key={`${row.documentationCode}-${idx}`}>
              <TableCell>{row.documentationCode}</TableCell>
              <TableCell>{row.brand ?? '—'}</TableCell>
              <TableCell align="right">{num1(row.plannedHours)}</TableCell>
              <TableCell align="right">{num1(row.actualHours)}</TableCell>
            </TableRow>
          ))}
          <TableRow sx={{ bgcolor: '#EEF4FF' }}>
            <TableCell colSpan={2} sx={{ fontWeight: 700 }}>
              ИТОГО
            </TableCell>
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {num1(planned)}
            </TableCell>
            <TableCell align="right" sx={{ fontWeight: 700 }}>
              {num1(actual)}
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </Box>
  );
}

function LaborBlock({ rows }: { rows: ProjectVersionLaborRow[] }) {
  const planned = rows.reduce((s, r) => s + (r.plannedHours ?? 0), 0);
  const actual = rows.reduce((s, r) => s + (r.actualHours ?? 0), 0);
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
              <TableCell align="right">План, ч</TableCell>
              <TableCell align="right">Факт, ч</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row, idx) => (
              <TableRow key={`${row.documentationCode}-${idx}`}>
                <TableCell>{row.documentationCode}</TableCell>
                <TableCell>{row.brand ?? '—'}</TableCell>
                <TableCell>{row.workDirectionName ?? '—'}</TableCell>
                <TableCell align="right">{num1(row.plannedHours)}</TableCell>
                <TableCell align="right">{num1(row.actualHours)}</TableCell>
              </TableRow>
            ))}
            <TableRow sx={{ bgcolor: '#EEF4FF' }}>
              <TableCell colSpan={3} sx={{ fontWeight: 700 }}>
                ИТОГО
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {num1(planned)}
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {num1(actual)}
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
        color: '#FFFFFF',
        px: 2,
        py: 1.5,
        borderRadius: 1,
        mb: 0.5,
        minWidth: GRID_MIN_WIDTH,
        fontSize: REPORT_COLUMN_HEADER_FONT_SIZE,
        fontWeight: 600,
        alignItems: 'end',
      }}
    >
      <ReportHeaderCell>Шифр</ReportHeaderCell>
      <ReportHeaderCell>Направление</ReportHeaderCell>
      <ReportHeaderCell align="right">Факт ФО</ReportHeaderCell>
      <ReportHeaderCell align="right">Факт ч</ReportHeaderCell>
      <ReportHeaderCell align="right" sx={{ color: OPT_COLOR }}>
        План.Труд.Оптим.
      </ReportHeaderCell>
      <ReportHeaderCell align="right">План.Труд.Баз.</ReportHeaderCell>
      <ReportHeaderCell align="right" sx={{ color: PES_COLOR }}>
        План.Труд.Песс.
      </ReportHeaderCell>
      <ReportHeaderCell align="right">Факт произв.</ReportHeaderCell>
      <ReportHeaderCell align="right">Норм. произв.</ReportHeaderCell>
      <ReportHeaderCell align="right">Z</ReportHeaderCell>
      <ReportHeaderCell>Интерпретация</ReportHeaderCell>
    </Box>
  );
}

function ReportDataRow({
  code,
  name,
  factFo,
  factHours,
  optH,
  baseH,
  pesH,
  factProd,
  normProd,
  zScore,
  interpretation,
  dark = false,
}: {
  code: string;
  name: string;
  factFo: unknown;
  factHours: unknown;
  optH: unknown;
  baseH: unknown;
  pesH: unknown;
  factProd: unknown;
  normProd: unknown;
  zScore: unknown;
  interpretation: string;
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
        minWidth: GRID_MIN_WIDTH,
        bgcolor: dark ? '#1E3A5F' : '#FFFFFF',
        color: dark ? '#FFFFFF' : '#2C3E50',
        border: dark ? 'none' : '1px solid #E2E8F0',
        borderRadius: 1,
        mb: dark ? 1 : 0,
        fontSize: dark ? 13 : 14,
        fontWeight: dark ? 700 : 400,
      }}
    >
      <Box sx={{ fontWeight: dark ? 700 : 600, fontFamily: code ? 'monospace' : 'inherit' }}>
        {code || (dark ? 'ИТОГО' : '—')}
      </Box>
      <Box sx={{ fontWeight: dark ? 600 : 500 }}>{name}</Box>
      <Box sx={{ textAlign: 'right' }}>
        {typeof factFo === 'string' ? factFo : dark ? (factFo != null && factFo !== '' ? String(factFo) : '—') : num2(factFo)}
      </Box>
      <Box sx={{ textAlign: 'right' }}>{num1(factHours)}</Box>
      <Box
        sx={{
          textAlign: 'right',
          fontWeight: 600,
          color: dark ? '#A9DFBF' : OPT_COLOR,
          bgcolor: dark ? 'transparent' : PLAN_HOUR_BG.optimistic,
          borderRadius: 0.5,
          px: 0.5,
        }}
      >
        {num1(optH)}
      </Box>
      <Box
        sx={{
          textAlign: 'right',
          fontWeight: 600,
          bgcolor: dark ? 'transparent' : PLAN_HOUR_BG.base,
          borderRadius: 0.5,
          px: 0.5,
        }}
      >
        {num1(baseH)}
      </Box>
      <Box
        sx={{
          textAlign: 'right',
          fontWeight: 600,
          color: dark ? '#F1948A' : PES_COLOR,
          bgcolor: dark ? 'transparent' : PLAN_HOUR_BG.pessimistic,
          borderRadius: 0.5,
          px: 0.5,
        }}
      >
        {num1(pesH)}
      </Box>
      <Box sx={{ textAlign: 'right' }}>{dark ? '—' : num4(factProd)}</Box>
      <Box sx={{ textAlign: 'right' }}>{dark ? '—' : num4(normProd)}</Box>
      <Box sx={{ textAlign: 'right' }}>{num4(zScore)}</Box>
      <Box>{interpretation}</Box>
    </Box>
  );
}

function directionTitle(d: { code: string; name: string; unitName: string | null }): string {
  return formatIisNorm({ code: d.code, name: d.name, unitName: d.unitName });
}

function interpretLabel(v: string | number | null | undefined): string {
  if (v == null) return '—';
  if (typeof v === 'number') return String(v);
  return v || '—';
}

export function FactAnalysisReportView({ projectId, versionId }: FactAnalysisReportViewProps) {
  const { canRead } = usePermission('Assessment');
  const [exporting, setExporting] = useState(false);

  const {
    data: report,
    isPending,
    isFetching,
    isError,
    error: queryError,
    refetch,
  } = useQuery({
    queryKey: queryKeys.reports.versionAnalysis(projectId, versionId),
    queryFn: () => fetchVersionAnalysis(projectId, versionId),
    enabled: canRead,
  });

  const loading = isPending || isFetching;
  const error = isError
    ? getApiErrorMessage(queryError, 'Не удалось загрузить отчёт анализа версии.')
    : null;

  const cardRows = useMemo(
    () => (report ? orderDirectionsForCards(report.directions) : []),
    [report],
  );

  const projectTotal = useMemo(() => {
    if (!report) return null;
    const allIds = cardRows.map((d) => d.id);
    const hours = sumFactCardHours(cardRows, allIds);
    const fo = (report.summary.physicalVolumeTotalsByUnit ?? [])
      .map((u) => `${num2(u.totalVolume)} ${u.unitName}`.trim())
      .filter(Boolean)
      .join('; ');
    return {
      fo: fo || '—',
      hours: report.summary.totalFactLaborHours,
      opt: hours.optimisticHours,
      base: hours.baseHours,
      pes: hours.pessimisticHours,
      z: report.summary.weightedZScore,
      interpretation: report.summary.weightedZScoreInterpretation,
    };
  }, [report, cardRows]);

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
          <ToolbarActionButton
            startIcon={<RefreshIcon />}
            disabled={loading || exporting}
            onClick={() => void refetch()}
          >
            {ACTION_LABELS.refresh}
          </ToolbarActionButton>
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
        </Stack>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 1 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ flex: 1, overflow: 'auto', minHeight: 0 }}>
        <ReportHeaderRow />
        {projectTotal && (
          <ReportDataRow
            code=""
            name="По проекту"
            factFo={projectTotal.fo}
            factHours={projectTotal.hours}
            optH={projectTotal.opt}
            baseH={projectTotal.base}
            pesH={projectTotal.pes}
            factProd={null}
            normProd={null}
            zScore={projectTotal.z}
            interpretation={interpretLabel(projectTotal.interpretation)}
            dark
          />
        )}
        {cardRows.map((d) => {
          const pv = ownVolumes(d, 'physicalVolumes');
          const eq = ownVolumes(d, 'equipmentVolumes');
          const labor = ownLabor(d);
          const factFo = factFoForCard(d, [...pv, ...eq]);
          const hasDetail = pv.length > 0 || eq.length > 0 || labor.length > 0;
          return (
            <Paper
              key={d.id}
              data-testid={`fact-analysis-card-${d.id}`}
              elevation={0}
              sx={{
                mb: 1.5,
                ml: `${Math.max(0, d.level) * 24}px`,
                border: '1px solid #E2E8F0',
                borderRadius: 1,
                overflow: 'hidden',
              }}
            >
              <ReportDataRow
                code={d.code || '—'}
                name={directionTitle(d)}
                factFo={factFo}
                factHours={d.factLaborHours}
                optH={d.optimisticHours}
                baseH={d.baseHours}
                pesH={d.pessimisticHours}
                factProd={d.factProductivity}
                normProd={d.normProductivity}
                zScore={d.zScore}
                interpretation={interpretLabel(d.interpretation)}
              />
              {d.unitName && (
                <Typography variant="caption" sx={{ color: '#64748B', display: 'block', px: 2, pt: 0.5 }}>
                  ед. изм.: <strong>{d.unitName}</strong>
                </Typography>
              )}
              {hasDetail && (
                <Box sx={{ px: 2, pb: 1.5, bgcolor: '#F8FAFC' }}>
                  <VolumeTable title="Физ. объёмы проекта" rows={pv} />
                  <VolumeTable title="Оборудование проекта" rows={eq} />
                  <DirectionLaborTable rows={labor} />
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
