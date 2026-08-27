import { useMemo, useState, type ReactNode } from 'react';
import {
  Alert,
  Box,
  Chip,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import HelpOutlineOutlinedIcon from '@mui/icons-material/HelpOutlineOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import { FactVsNormChart } from '../../features/analytics/factNorm/FactVsNormChart';
import {
  deltaHeatBackground,
  severityColor,
  severityLabel,
  severityRatio,
} from '../../features/analytics/factNorm/severity';
import type {
  ChartPoint,
  Confidence,
  NormComparison,
  Verdict,
} from '../../api/factNormAnalyticsApi';

const VERDICT_LABEL: Record<Verdict, string> = {
  NormIsActual: 'Норма актуальна',
  AdjustParameters: 'Скорректировать параметры',
  ChangeFormulaType: 'Изменить тип формулы',
  InsufficientData: 'Недостаточно данных',
};

const VERDICT_COLOR: Record<Verdict, 'success' | 'warning' | 'error' | 'default'> = {
  NormIsActual: 'success',
  AdjustParameters: 'warning',
  ChangeFormulaType: 'error',
  InsufficientData: 'default',
};

const CONFIDENCE_LABEL: Record<Confidence, string> = {
  Low: 'Низкая',
  Medium: 'Средняя',
  High: 'Высокая',
};

/** Генерирует облако точек факта вокруг линейной нормы y = a*x + b с заданным шумом и долей выбросов. */
function generatePoints(
  a: number,
  b: number,
  count: number,
  noiseSigma: number,
  outlierShare: number,
  seedOffset: number,
): ChartPoint[] {
  const points: ChartPoint[] = [];
  for (let i = 0; i < count; i++) {
    const x = 0.5 + (i / count) * 4.5;
    const base = a * x + b;
    // Простой детерминированный "шум" вместо Math.random(), чтобы мокап был стабильным между рендерами.
    const wobble = Math.sin((i + seedOffset) * 12.9898) * 43758.5453;
    const frac = wobble - Math.floor(wobble);
    const isOutlier = i / count < outlierShare;
    const noise = (frac - 0.5) * 2 * (isOutlier ? noiseSigma * 3.2 : noiseSigma);
    points.push({
      x: Number(x.toFixed(2)),
      y: Number((base + noise).toFixed(4)),
      projectCode: `PRJ-${1000 + seedOffset + i}`,
      projectName: `Проект «${['Альфа', 'Вектор', 'Гранит', 'Дельта', 'Кристалл', 'Меридиан'][i % 6]}»`,
      versionName: `v${1 + (i % 3)}.${i % 5}`,
    });
  }
  return points;
}

interface MockGroup {
  normId: number;
  workDirectionId: number;
  groupName: string;
  verdict: Verdict;
  confidence: Confidence | null;
  a: number;
  b: number;
  noiseSigma: number;
  outlierShare: number;
  proposedA?: number;
  proposedB?: number;
  currentR2: number;
  proposedR2: number;
}

const MOCK_GROUPS: MockGroup[] = [
  {
    normId: 101,
    workDirectionId: 1,
    groupName: 'Разработка ПО — кодирование',
    verdict: 'NormIsActual',
    confidence: null,
    a: 0.42,
    b: 0.12,
    noiseSigma: 0.05,
    outlierShare: 0.05,
    currentR2: 0.91,
    proposedR2: 0.91,
  },
  {
    normId: 102,
    workDirectionId: 2,
    groupName: 'Проектирование конструкций — расчётная часть',
    verdict: 'NormIsActual',
    confidence: null,
    a: 0.35,
    b: 0.2,
    noiseSigma: 0.04,
    outlierShare: 0.03,
    currentR2: 0.89,
    proposedR2: 0.89,
  },
  {
    normId: 103,
    workDirectionId: 3,
    groupName: 'Электротехнические системы — рабочая документация',
    verdict: 'AdjustParameters',
    confidence: 'High',
    a: 0.3,
    b: 0.15,
    noiseSigma: 0.09,
    outlierShare: 0.1,
    proposedA: 0.38,
    proposedB: 0.22,
    currentR2: 0.71,
    proposedR2: 0.88,
  },
  {
    normId: 104,
    workDirectionId: 4,
    groupName: 'Генеральный план и благоустройство',
    verdict: 'AdjustParameters',
    confidence: 'Medium',
    a: 0.28,
    b: 0.1,
    noiseSigma: 0.11,
    outlierShare: 0.14,
    proposedA: 0.33,
    proposedB: 0.16,
    currentR2: 0.64,
    proposedR2: 0.79,
  },
  {
    normId: 105,
    workDirectionId: 5,
    groupName: 'Инженерные сети — водоснабжение и водоотведение',
    verdict: 'ChangeFormulaType',
    confidence: 'High',
    a: 0.18,
    b: 0.35,
    noiseSigma: 0.16,
    outlierShare: 0.24,
    proposedA: 0.45,
    proposedB: -0.05,
    currentR2: 0.38,
    proposedR2: 0.82,
  },
  {
    normId: 106,
    workDirectionId: 6,
    groupName: 'BIM-координация и коллизии',
    verdict: 'InsufficientData',
    confidence: 'Low',
    a: 0.4,
    b: 0.1,
    noiseSigma: 0.07,
    outlierShare: 0.08,
    currentR2: 0.55,
    proposedR2: 0.55,
  },
  {
    normId: 107,
    workDirectionId: 7,
    groupName: 'Пожарная безопасность — расчёты рисков',
    verdict: 'AdjustParameters',
    confidence: 'Medium',
    a: 0.25,
    b: 0.3,
    noiseSigma: 0.1,
    outlierShare: 0.12,
    proposedA: 0.29,
    proposedB: 0.34,
    currentR2: 0.68,
    proposedR2: 0.8,
  },
  {
    normId: 108,
    workDirectionId: 8,
    groupName: 'Технологические решения производства',
    verdict: 'NormIsActual',
    confidence: null,
    a: 0.5,
    b: 0.05,
    noiseSigma: 0.06,
    outlierShare: 0.04,
    currentR2: 0.93,
    proposedR2: 0.93,
  },
];

function buildComparison(g: MockGroup): NormComparison {
  const points = generatePoints(g.a, g.b, 22, g.noiseSigma, g.outlierShare, g.normId);
  const ys = points.map((p) => p.y);
  const factMean = ys.reduce((s, v) => s + v, 0) / ys.length;
  const variance = ys.reduce((s, v) => s + (v - factMean) ** 2, 0) / ys.length;
  const factStd = Math.sqrt(variance);
  // "Норма" — теоретическое среднее по формуле в центре диапазона X, σ нормы масштабируется от заданного шума.
  const normMean = g.a * 2.5 + g.b;
  const normStd = Math.max(g.noiseSigma * 1.1, 0.02);
  const deltaMean = factMean - normMean;
  const deltaSigma = factStd - normStd;
  const deltaR2 = g.proposedR2 - g.currentR2;

  return {
    normId: g.normId,
    workDirectionId: g.workDirectionId,
    groupName: g.groupName,
    currentFormula: 'linear',
    proposedFormula: 'linear',
    deltaMean,
    deltaSigma,
    currentR2: g.currentR2,
    proposedR2: g.proposedR2,
    deltaR2,
    verdict: g.verdict,
    requiresAttention: g.verdict === 'AdjustParameters' || g.verdict === 'ChangeFormulaType',
    insufficientData: g.verdict === 'InsufficientData',
    sigmaZero: false,
    suggestionId: g.verdict === 'NormIsActual' || g.verdict === 'InsufficientData' ? null : g.normId,
    chartPoints: points,
    currentParameters: { a: g.a, b: g.b },
    proposedParameters:
      g.proposedA != null && g.proposedB != null ? { a: g.proposedA, b: g.proposedB } : null,
    warning: null,
    normMeanPerformance: normMean,
    factMeanPerformance: factMean,
    factStdDeviation: factStd,
    normStdDeviation: normStd,
  };
}

const MOCK_COMPARISONS: NormComparison[] = MOCK_GROUPS.map(buildComparison);

type KpiFilter = 'all' | 'actual' | 'attention' | 'insufficient';

export default function FactNormAnalysisMockup() {
  const [selected, setSelected] = useState<NormComparison | null>(MOCK_COMPARISONS[2] ?? null);
  const [kpiFilter, setKpiFilter] = useState<KpiFilter>('all');
  const [editRow, setEditRow] = useState<NormComparison | null>(null);

  const summary = useMemo(() => {
    const total = MOCK_COMPARISONS.length;
    const actual = MOCK_COMPARISONS.filter((c) => c.verdict === 'NormIsActual').length;
    const attention = MOCK_COMPARISONS.filter(
      (c) => c.verdict === 'AdjustParameters' || c.verdict === 'ChangeFormulaType',
    ).length;
    const insufficient = MOCK_COMPARISONS.filter((c) => c.verdict === 'InsufficientData').length;
    const hasCriticalAttention = MOCK_COMPARISONS.some(
      (c) => c.verdict === 'ChangeFormulaType' || severityRatio(c) >= 2,
    );
    return { total, actual, attention, insufficient, hasCriticalAttention };
  }, []);

  const kpiCards: Array<{
    key: string;
    filterKey: KpiFilter;
    label: string;
    value: number;
    icon: ReactNode;
    color: string;
  }> = [
    {
      key: 'total',
      filterKey: 'all',
      label: 'Групп проверено',
      value: summary.total,
      icon: <AssessmentOutlinedIcon fontSize="small" />,
      color: 'text.secondary',
    },
    {
      key: 'actual',
      filterKey: 'actual',
      label: 'Норма актуальна',
      value: summary.actual,
      icon: <CheckCircleOutlineIcon fontSize="small" />,
      color: 'success.main',
    },
    {
      key: 'attention',
      filterKey: 'attention',
      label: 'Требуют внимания',
      value: summary.attention,
      icon: <WarningAmberOutlinedIcon fontSize="small" />,
      color: summary.hasCriticalAttention ? 'error.main' : 'warning.main',
    },
    {
      key: 'insufficient',
      filterKey: 'insufficient',
      label: 'Недостаточно данных',
      value: summary.insufficient,
      icon: <HelpOutlineOutlinedIcon fontSize="small" />,
      color: 'text.secondary',
    },
  ];

  const filteredComparisons = useMemo(() => {
    if (kpiFilter === 'all') return MOCK_COMPARISONS;
    if (kpiFilter === 'actual') return MOCK_COMPARISONS.filter((c) => c.verdict === 'NormIsActual');
    if (kpiFilter === 'insufficient') {
      return MOCK_COMPARISONS.filter((c) => c.verdict === 'InsufficientData');
    }
    return MOCK_COMPARISONS.filter(
      (c) => c.verdict === 'ChangeFormulaType' || c.verdict === 'AdjustParameters',
    );
  }, [kpiFilter]);

  return (
    <Box sx={{ height: '100%', overflowY: 'auto', bgcolor: 'background.default' }}>
    <Box sx={{ p: 3, display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Box>
        <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
          Сравнение факта и норм
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Мокап переработанного дизайна: серьёзность отклонений, тепловая подсветка, редактирование нормы из любой
          строки, фильтрация по KPI-карточкам.
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        {kpiCards.map((card) => {
          const active = kpiFilter === card.filterKey;
          return (
            <Paper
              key={card.key}
              component="button"
              type="button"
              variant="outlined"
              onClick={() => setKpiFilter(active ? 'all' : card.filterKey)}
              aria-pressed={active}
              sx={{
                flex: '1 1 220px',
                minWidth: 200,
                p: 2,
                display: 'flex',
                alignItems: 'center',
                gap: 1.5,
                textAlign: 'left',
                cursor: 'pointer',
                font: 'inherit',
                borderColor: active ? 'primary.main' : undefined,
                borderWidth: active ? 2 : 1,
                backgroundColor: active ? 'action.selected' : 'background.paper',
                transition: 'background-color 0.15s, border-color 0.15s',
                '&:hover': { backgroundColor: 'action.hover' },
              }}
            >
              <Box sx={{ color: card.color, display: 'flex' }}>{card.icon}</Box>
              <Box>
                <Typography variant="h6" component="div" sx={{ color: card.color, lineHeight: 1.2 }}>
                  {card.value}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {card.label}
                </Typography>
              </Box>
            </Paper>
          );
        })}
      </Box>

      {kpiFilter !== 'all' && (
        <Alert severity="info" onClose={() => setKpiFilter('all')} sx={{ py: 0 }}>
          Показаны только строки: {kpiCards.find((c) => c.filterKey === kpiFilter)?.label}. Нажмите на карточку ещё
          раз или на крестик, чтобы вернуться к полному списку.
        </Alert>
      )}

      <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 560 }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell sx={{ minWidth: 220 }}>Группа IIC</TableCell>
              <TableCell sx={{ minWidth: 150 }}>Серьёзность</TableCell>
              <TableCell align="right">μ нормы</TableCell>
              <TableCell align="right">Среднее факт</TableCell>
              <TableCell align="right">Δμ (факт−норма)</TableCell>
              <TableCell align="right">σ нормы</TableCell>
              <TableCell align="right">σ факт</TableCell>
              <TableCell align="right">Δσ</TableCell>
              <TableCell align="right">ΔR²</TableCell>
              <TableCell sx={{ minWidth: 190 }}>Вердикт</TableCell>
              <TableCell align="center" sx={{ width: 56 }} />
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredComparisons.map((row) => {
              const ratio = severityRatio(row);
              const isSelected = selected?.normId === row.normId;
              return (
                <TableRow
                  key={row.normId}
                  hover
                  selected={isSelected}
                  onClick={() => setSelected(row)}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell sx={{ fontWeight: isSelected ? 600 : 400 }}>{row.groupName}</TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.75} alignItems="center">
                      <Box
                        sx={{
                          width: 9,
                          height: 9,
                          borderRadius: '50%',
                          flexShrink: 0,
                          bgcolor: severityColor(ratio),
                        }}
                      />
                      <Typography variant="body2" noWrap>
                        {severityLabel(ratio)}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell align="right">{row.normMeanPerformance.toFixed(3)}</TableCell>
                  <TableCell align="right">{row.factMeanPerformance.toFixed(3)}</TableCell>
                  <TableCell
                    align="right"
                    sx={{
                      backgroundColor: deltaHeatBackground(
                        row.deltaMean,
                        row.normStdDeviation || row.factStdDeviation || 1,
                      ),
                    }}
                  >
                    {row.deltaMean.toFixed(3)}
                  </TableCell>
                  <TableCell align="right">{row.normStdDeviation.toFixed(3)}</TableCell>
                  <TableCell align="right">{row.factStdDeviation.toFixed(3)}</TableCell>
                  <TableCell
                    align="right"
                    sx={{ backgroundColor: deltaHeatBackground(row.deltaSigma, row.normStdDeviation || 1) }}
                  >
                    {row.deltaSigma.toFixed(3)}
                  </TableCell>
                  <TableCell align="right" sx={{ backgroundColor: deltaHeatBackground(row.deltaR2, 0.3) }}>
                    {row.deltaR2.toFixed(3)}
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={VERDICT_COLOR[row.verdict]}
                      label={VERDICT_LABEL[row.verdict]}
                      aria-label={VERDICT_LABEL[row.verdict]}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Изменить норму">
                      <IconButton
                        size="small"
                        aria-label={`Изменить норму: ${row.groupName}`}
                        onClick={(e) => {
                          e.stopPropagation();
                          setEditRow(row);
                        }}
                      >
                        <EditOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      {selected && (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Chip size="small" color={VERDICT_COLOR[selected.verdict]} label={VERDICT_LABEL[selected.verdict]} />
            <Chip
              size="small"
              variant="outlined"
              label={`Отклонение: ${severityLabel(severityRatio(selected))}`}
              sx={{
                borderColor: severityColor(severityRatio(selected)),
                color: severityColor(severityRatio(selected)),
              }}
            />
            {selected.suggestionId && MOCK_GROUPS.find((g) => g.normId === selected.normId)?.confidence ? (
              <Chip
                size="small"
                variant="outlined"
                label={`Уверенность предложения: ${
                  CONFIDENCE_LABEL[MOCK_GROUPS.find((g) => g.normId === selected.normId)!.confidence!]
                }`}
              />
            ) : null}
            <Tooltip title="Изменить норму этой группы">
              <IconButton size="small" onClick={() => setEditRow(selected)} aria-label="Изменить норму">
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
          <FactVsNormChart
            title={`${selected.groupName}: производительность vs объём`}
            points={selected.chartPoints ?? []}
            currentFormula={selected.currentFormula}
            currentParams={selected.currentParameters}
            proposedFormula={selected.proposedFormula}
            proposedParams={selected.proposedParameters}
          />
        </Box>
      )}

      {editRow && (
        <Paper
          variant="outlined"
          sx={{ p: 2, borderColor: 'primary.main', borderWidth: 2, display: 'flex', flexDirection: 'column', gap: 1 }}
        >
          <Typography variant="subtitle2">
            Мокап: редактирование нормы «{editRow.groupName}» (в рабочей версии — полноценный NormFormulaEditor +
            DeviationFormulaEditor с live-предпросмотром полосы допуска)
          </Typography>
          <Stack direction="row" spacing={2}>
            <Typography variant="body2" color="text.secondary">
              Текущие параметры: a={editRow.currentParameters?.a?.toFixed(2)}, b=
              {editRow.currentParameters?.b?.toFixed(2)}
            </Typography>
            {editRow.proposedParameters && (
              <Typography variant="body2" color="text.secondary">
                Предложение: a={editRow.proposedParameters.a?.toFixed(2)}, b=
                {editRow.proposedParameters.b?.toFixed(2)}
              </Typography>
            )}
          </Stack>
          <Box>
            <IconButton size="small" onClick={() => setEditRow(null)} aria-label="Закрыть">
              <Typography variant="button">Закрыть</Typography>
            </IconButton>
          </Box>
        </Paper>
      )}
    </Box>
    </Box>
  );
}
