import { useMemo, useState, type ReactNode } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Slider,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import ShowChartOutlinedIcon from '@mui/icons-material/ShowChartOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined';
import HelpOutlineOutlinedIcon from '@mui/icons-material/HelpOutlineOutlined';
import GridViewOutlinedIcon from '@mui/icons-material/GridViewOutlined';
import TableRowsOutlinedIcon from '@mui/icons-material/TableRowsOutlined';
import CompareArrowsOutlinedIcon from '@mui/icons-material/CompareArrowsOutlined';
import RestartAltOutlinedIcon from '@mui/icons-material/RestartAltOutlined';
import { NormBandChart } from '../../features/norms/NormBandChart';
import type { NormPreviewPoint } from '../../api/normsApi';

type IssueSeverity = 'Error' | 'Warning' | 'Info' | null;
type NormStatus = 'Active' | 'Historical' | 'Future';
type Confidence = 'High' | 'Medium' | 'Low';

interface MockNorm {
  id: number;
  category: string;
  groupName: string;
  formulaType: string;
  mean: number;
  sigma: number;
  status: NormStatus;
  effectiveFrom: string;
  effectiveTo: string | null;
  issueSeverity: IssueSeverity;
  issueMessage: string | null;
  issueRecommendation: string | null;
  previousMean: number | null;
  previousSigma: number | null;
  lastReviewedMonthsAgo: number;
  sampleSize: number;
  rSquared: number;
  /** Отношение факт/норма за последние 6 периодов — для мини-графика дрейфа. */
  driftHistory: number[];
}

const SEVERITY_RANK: Record<'Error' | 'Warning' | 'Info' | 'None', number> = {
  Error: 3,
  Warning: 2,
  Info: 1,
  None: 0,
};

const SEVERITY_COLOR: Record<'Error' | 'Warning' | 'Info' | 'None', string> = {
  Error: '#E74C3C',
  Warning: '#F39C12',
  Info: '#3498DB',
  None: '#27AE60',
};

const SEVERITY_BG: Record<'Error' | 'Warning' | 'Info' | 'None', string> = {
  Error: 'rgba(231, 76, 60, 0.16)',
  Warning: 'rgba(243, 156, 18, 0.16)',
  Info: 'rgba(52, 152, 219, 0.14)',
  None: 'rgba(39, 174, 96, 0.12)',
};

const SEVERITY_LABEL: Record<'Error' | 'Warning' | 'Info' | 'None', string> = {
  Error: 'Требует пересмотра',
  Warning: 'Есть отклонение',
  Info: 'Незначительное замечание',
  None: 'В норме',
};

const STATUS_LABEL: Record<NormStatus, string> = {
  Active: 'Активна',
  Historical: 'Историческая',
  Future: 'Будущая',
};

const CONFIDENCE_COLOR: Record<Confidence, string> = {
  High: '#27AE60',
  Medium: '#F39C12',
  Low: '#E74C3C',
};

const CONFIDENCE_LABEL: Record<Confidence, string> = {
  High: 'Высокая',
  Medium: 'Средняя',
  Low: 'Низкая',
};

function severityKey(s: IssueSeverity): 'Error' | 'Warning' | 'Info' | 'None' {
  return s ?? 'None';
}

function confidenceOf(n: MockNorm): Confidence {
  if (n.sampleSize >= 20 && n.rSquared >= 0.7) return 'High';
  if (n.sampleSize >= 8 && n.rSquared >= 0.4) return 'Medium';
  return 'Low';
}

const RAW_NORMS: MockNorm[] = [
  {
    id: 101,
    category: 'Разработка ПО',
    groupName: 'Разработка ПО — кодирование',
    formulaType: 'Linear',
    mean: 1.42,
    sigma: 0.09,
    status: 'Active',
    effectiveFrom: '2025-03-01',
    effectiveTo: null,
    issueSeverity: null,
    issueMessage: null,
    issueRecommendation: null,
    previousMean: 1.3,
    previousSigma: 0.11,
    lastReviewedMonthsAgo: 5,
    sampleSize: 34,
    rSquared: 0.86,
    driftHistory: [0.97, 1.01, 0.99, 1.02, 1.0, 1.01],
  },
  {
    id: 102,
    category: 'Проектирование конструкций',
    groupName: 'Проектирование конструкций — расчётная часть',
    formulaType: 'Power',
    mean: 0.87,
    sigma: 0.05,
    status: 'Active',
    effectiveFrom: '2025-01-15',
    effectiveTo: null,
    issueSeverity: 'Info',
    issueMessage: 'Мало версий факта для устойчивой оценки σ (< 8 версий).',
    issueRecommendation: 'Дождаться накопления данных, пересмотр не требуется сейчас.',
    previousMean: 0.85,
    previousSigma: 0.06,
    lastReviewedMonthsAgo: 7,
    sampleSize: 6,
    rSquared: 0.52,
    driftHistory: [1.05, 0.98, 1.1, 0.95, 1.03, 1.04],
  },
  {
    id: 103,
    category: 'Электротехнические системы',
    groupName: 'Электротехнические системы — рабочая документация',
    formulaType: 'Linear',
    mean: 0.61,
    sigma: 0.24,
    status: 'Active',
    effectiveFrom: '2024-11-01',
    effectiveTo: null,
    issueSeverity: 'Warning',
    issueMessage: 'Разброс факта (σ) вдвое выше типичного для этого типа формулы.',
    issueRecommendation: 'Проверить корректировку параметров формулы во вкладке «Сравнение факта и норм».',
    previousMean: 0.6,
    previousSigma: 0.19,
    lastReviewedMonthsAgo: 10,
    sampleSize: 22,
    rSquared: 0.48,
    driftHistory: [1.02, 1.08, 1.14, 1.11, 1.18, 1.22],
  },
  {
    id: 104,
    category: 'Генплан',
    groupName: 'Генеральный план и благоустройство',
    formulaType: 'Logarithmic',
    mean: 0.33,
    sigma: 0.02,
    status: 'Historical',
    effectiveFrom: '2023-06-01',
    effectiveTo: '2025-01-01',
    issueSeverity: null,
    issueMessage: null,
    issueRecommendation: null,
    previousMean: null,
    previousSigma: null,
    lastReviewedMonthsAgo: 19,
    sampleSize: 41,
    rSquared: 0.91,
    driftHistory: [1.0, 0.99, 1.01, 1.0, 0.98, 1.0],
  },
  {
    id: 105,
    category: 'Инженерные сети',
    groupName: 'Инженерные сети — водоснабжение и водоотведение',
    formulaType: 'ShiftedLog',
    mean: 0.95,
    sigma: 0.41,
    status: 'Active',
    effectiveFrom: '2025-02-01',
    effectiveTo: null,
    issueSeverity: 'Error',
    issueMessage: 'σ почти равна μ — формула не описывает характер данных, высокий риск ложных срабатываний.',
    issueRecommendation: 'Изменить тип формулы: см. предложение во вкладке «Сравнение факта и норм».',
    previousMean: 0.9,
    previousSigma: 0.22,
    lastReviewedMonthsAgo: 4,
    sampleSize: 17,
    rSquared: 0.29,
    driftHistory: [1.1, 1.24, 1.15, 1.35, 1.28, 1.41],
  },
  {
    id: 106,
    category: 'BIM',
    groupName: 'BIM-координация и коллизии',
    formulaType: 'Constant',
    mean: 0.5,
    sigma: 0.03,
    status: 'Future',
    effectiveFrom: '2026-10-01',
    effectiveTo: null,
    issueSeverity: 'Info',
    issueMessage: 'Норма вступит в силу 01.10.2026 — пока не влияет на текущие расчёты.',
    issueRecommendation: null,
    previousMean: 0.48,
    previousSigma: 0.04,
    lastReviewedMonthsAgo: 1,
    sampleSize: 9,
    rSquared: 0.61,
    driftHistory: [1.0, 1.01, 0.99, 1.0, 1.0, 0.99],
  },
  {
    id: 107,
    category: 'Пожарная безопасность',
    groupName: 'Пожарная безопасность — расчёты рисков',
    formulaType: 'Polynomial',
    mean: 0.44,
    sigma: 0.18,
    status: 'Active',
    effectiveFrom: '2024-06-01',
    effectiveTo: null,
    issueSeverity: 'Warning',
    issueMessage: 'Норма не пересматривалась 14 месяцев при активном использовании.',
    issueRecommendation: 'Запланировать плановую ревизию параметров.',
    previousMean: 0.44,
    previousSigma: 0.16,
    lastReviewedMonthsAgo: 14,
    sampleSize: 28,
    rSquared: 0.58,
    driftHistory: [1.05, 1.02, 1.08, 1.04, 1.09, 1.07],
  },
  {
    id: 108,
    category: 'Технологические решения',
    groupName: 'Технологические решения производства',
    formulaType: 'Exponential',
    mean: 1.1,
    sigma: 0.06,
    status: 'Active',
    effectiveFrom: '2025-04-01',
    effectiveTo: null,
    issueSeverity: null,
    issueMessage: null,
    issueRecommendation: null,
    previousMean: 1.08,
    previousSigma: 0.07,
    lastReviewedMonthsAgo: 3,
    sampleSize: 31,
    rSquared: 0.83,
    driftHistory: [1.0, 0.99, 1.02, 1.01, 0.98, 1.0],
  },
  {
    id: 109,
    category: 'Архитектурные решения',
    groupName: 'Архитектурные решения — рабочая документация',
    formulaType: 'Linear',
    mean: 0.78,
    sigma: 0.05,
    status: 'Active',
    effectiveFrom: '2025-05-01',
    effectiveTo: null,
    issueSeverity: null,
    issueMessage: null,
    issueRecommendation: null,
    previousMean: 0.76,
    previousSigma: 0.05,
    lastReviewedMonthsAgo: 2,
    sampleSize: 26,
    rSquared: 0.79,
    driftHistory: [0.99, 1.0, 1.01, 1.0, 0.99, 1.0],
  },
  {
    id: 110,
    category: 'Сметная документация',
    groupName: 'Сметная документация — проверка',
    formulaType: 'Hyperbolic',
    mean: 0.29,
    sigma: 0.19,
    status: 'Active',
    effectiveFrom: '2025-06-01',
    effectiveTo: null,
    issueSeverity: 'Error',
    issueMessage: 'σ выросла втрое относительно предыдущей версии нормы (0.06 → 0.19).',
    issueRecommendation: 'Проверить, не попали ли в выборку аномальные проекты; пересчитать параметры.',
    previousMean: 0.3,
    previousSigma: 0.06,
    lastReviewedMonthsAgo: 3,
    sampleSize: 5,
    rSquared: 0.21,
    driftHistory: [1.15, 1.3, 1.2, 1.45, 1.38, 1.5],
  },
];

/** Направления работ (КФО), для которых в реестре нет ни одной активной нормы — «слепые зоны». */
const MISSING_NORM_GROUPS: string[] = [
  'Инженерная геология и геодезия',
  'Ландшафтное проектирование',
  'Экологическая экспертиза',
];

const MEDIAN_CV = (() => {
  const cvs = RAW_NORMS.map((n) => n.sigma / n.mean).sort((a, b) => a - b);
  const mid = Math.floor(cvs.length / 2);
  return cvs.length % 2 !== 0 ? cvs[mid] : (cvs[mid - 1] + cvs[mid]) / 2;
})();

function cvHeatBackground(cv: number): string {
  const ratio = cv / (MEDIAN_CV || 0.01);
  if (ratio >= 2.2) return SEVERITY_BG.Error;
  if (ratio >= 1.5) return SEVERITY_BG.Warning;
  return 'transparent';
}

function generateBand(mean: number, ratio: number): NormPreviewPoint[] {
  const points: NormPreviewPoint[] = [];
  for (let i = 0; i <= 10; i++) {
    const volume = 0.5 + i * 0.45;
    const center = mean * (0.72 + 0.28 * (i / 10));
    points.push({
      volume: Number(volume.toFixed(2)),
      center: Number(center.toFixed(4)),
      lower: Number((center * (1 - ratio)).toFixed(4)),
      upper: Number((center * (1 + ratio)).toFixed(4)),
    });
  }
  return points;
}

/** Детерминированный псевдо-разброс исторических фактических точек вокруг μ нормы (для симулятора). */
function syntheticFactPoints(n: MockNorm): number[] {
  const base = n.mean;
  const spread = n.sigma;
  const seedFactors = [-1.6, -1.1, -0.6, -0.3, -0.1, 0.1, 0.25, 0.5, 0.8, 1.2, 1.5, 1.9];
  return seedFactors.map((f) => Number((base + f * spread).toFixed(4)));
}

function formatRuDate(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}.${m}.${y}`;
}

type KpiFilter = 'all' | 'Error' | 'Warning' | 'cv' | 'ok' | 'lowConfidence';
type ViewMode = 'table' | 'heatmap';

function Sparkline({ values, color }: { values: number[]; color: string }) {
  const w = 72;
  const h = 24;
  const min = Math.min(...values, 1) * 0.95;
  const max = Math.max(...values, 1) * 1.05;
  const range = max - min || 1;
  const points = values
    .map((v, i) => {
      const x = (i / (values.length - 1)) * w;
      const y = h - ((v - min) / range) * h;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(' ');
  const baselineY = h - ((1 - min) / range) * h;
  return (
    <Tooltip title={`Факт/норма за 6 периодов: ${values.map((v) => v.toFixed(2)).join(' → ')}`}>
      <svg width={w} height={h} role="img" aria-label="Динамика отношения факт/норма">
        <line x1={0} y1={baselineY} x2={w} y2={baselineY} stroke="currentColor" strokeOpacity={0.25} strokeDasharray="2,2" />
        <polyline points={points} fill="none" stroke={color} strokeWidth={1.75} strokeLinejoin="round" strokeLinecap="round" />
        <circle cx={w} cy={h - ((values[values.length - 1] - min) / range) * h} r={2.25} fill={color} />
      </svg>
    </Tooltip>
  );
}

export default function NormAnalysisMockup() {
  const [kpiFilter, setKpiFilter] = useState<KpiFilter>('all');
  const [selectedId, setSelectedId] = useState<number | null>(105);
  const [viewMode, setViewMode] = useState<ViewMode>('table');
  const [meanOffsetPct, setMeanOffsetPct] = useState(0);
  const [sigmaMultiplier, setSigmaMultiplier] = useState(1);

  const sorted = useMemo(
    () =>
      [...RAW_NORMS].sort((a, b) => {
        const rankDiff = SEVERITY_RANK[severityKey(b.issueSeverity)] - SEVERITY_RANK[severityKey(a.issueSeverity)];
        if (rankDiff !== 0) return rankDiff;
        return a.groupName.localeCompare(b.groupName, 'ru');
      }),
    [],
  );

  const summary = useMemo(() => {
    const total = RAW_NORMS.length;
    const errors = RAW_NORMS.filter((n) => n.issueSeverity === 'Error').length;
    const warnings = RAW_NORMS.filter((n) => n.issueSeverity === 'Warning').length;
    const cvOutliers = RAW_NORMS.filter((n) => n.sigma / n.mean >= MEDIAN_CV * 1.5).length;
    const ok = RAW_NORMS.filter((n) => n.issueSeverity === null).length;
    const lowConfidence = RAW_NORMS.filter((n) => confidenceOf(n) === 'Low').length;
    return { total, errors, warnings, cvOutliers, ok, lowConfidence };
  }, []);

  const filtered = useMemo(() => {
    if (kpiFilter === 'all') return sorted;
    if (kpiFilter === 'ok') return sorted.filter((n) => n.issueSeverity === null);
    if (kpiFilter === 'cv') return sorted.filter((n) => n.sigma / n.mean >= MEDIAN_CV * 1.5);
    if (kpiFilter === 'lowConfidence') return sorted.filter((n) => confidenceOf(n) === 'Low');
    return sorted.filter((n) => n.issueSeverity === kpiFilter);
  }, [sorted, kpiFilter]);

  const grouped = useMemo(() => {
    const map = new Map<string, MockNorm[]>();
    filtered.forEach((n) => {
      const list = map.get(n.category) ?? [];
      list.push(n);
      map.set(n.category, list);
    });
    return Array.from(map.entries());
  }, [filtered]);

  const selected = RAW_NORMS.find((n) => n.id === selectedId) ?? null;

  // Сброс параметров симулятора при переключении выбранной нормы.
  const [simFor, setSimFor] = useState<number | null>(null);
  if (selected && simFor !== selected.id) {
    setSimFor(selected.id);
    setMeanOffsetPct(0);
    setSigmaMultiplier(1);
  }

  const kpiCards: Array<{
    key: KpiFilter;
    label: string;
    value: number;
    icon: ReactNode;
    color: string;
  }> = [
    {
      key: 'all',
      label: 'Всего норм',
      value: summary.total,
      icon: <AssessmentOutlinedIcon fontSize="small" />,
      color: 'text.secondary',
    },
    {
      key: 'Error',
      label: 'Ошибки',
      value: summary.errors,
      icon: <ErrorOutlineIcon fontSize="small" />,
      color: 'error.main',
    },
    {
      key: 'Warning',
      label: 'Предупреждения',
      value: summary.warnings,
      icon: <WarningAmberOutlinedIcon fontSize="small" />,
      color: 'warning.main',
    },
    {
      key: 'cv',
      label: 'Аномальный разброс (CV)',
      value: summary.cvOutliers,
      icon: <ShowChartOutlinedIcon fontSize="small" />,
      color: '#8E44AD',
    },
    {
      key: 'lowConfidence',
      label: 'Низкая уверенность',
      value: summary.lowConfidence,
      icon: <HelpOutlineOutlinedIcon fontSize="small" />,
      color: '#8E44AD',
    },
    {
      key: 'ok',
      label: 'Без замечаний',
      value: summary.ok,
      icon: <CheckCircleOutlineIcon fontSize="small" />,
      color: 'success.main',
    },
  ];

  const today = new Date('2026-08-27');
  const timelineMinMs = new Date('2023-01-01').getTime();
  const timelineMaxMs = new Date('2027-06-01').getTime();
  const timelineSpan = timelineMaxMs - timelineMinMs;
  const todayLeftPct = ((today.getTime() - timelineMinMs) / timelineSpan) * 100;

  // Данные симулятора «что если» для выбранной нормы.
  const simData = useMemo(() => {
    if (!selected) return null;
    const baseRatio = selected.sigma / selected.mean;
    const factPoints = syntheticFactPoints(selected);

    const withinTolerance = (mean: number, ratio: number) => {
      const lower = mean * (1 - ratio);
      const upper = mean * (1 + ratio);
      const inCount = factPoints.filter((p) => p >= lower && p <= upper).length;
      return Math.round((inCount / factPoints.length) * 100);
    };

    const beforePct = withinTolerance(selected.mean, baseRatio);

    const newMean = selected.mean * (1 + meanOffsetPct / 100);
    const newRatio = baseRatio * sigmaMultiplier;
    const afterPct = withinTolerance(newMean, newRatio);

    return {
      currentBand: generateBand(selected.mean, baseRatio),
      proposedBand: generateBand(newMean, newRatio),
      beforePct,
      afterPct,
      newMean,
      newRatio,
    };
  }, [selected, meanOffsetPct, sigmaMultiplier]);

  return (
    <Box sx={{ height: '100%', overflowY: 'auto', bgcolor: 'background.default' }}>
      <Box sx={{ p: 3, display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 1280, mx: 'auto' }}>
        <Box>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
            Анализ норм
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Мокап переработанного дизайна: покрытие направлений нормами, достоверность (N, R²), дрейф-спарклайны,
            group heatmap-обзор, симулятор «что если» на формуле и переход к вкладке «Сравнение факта и норм».
          </Typography>
        </Box>

        {/* Блок 1: покрытие и пробелы */}
        {MISSING_NORM_GROUPS.length > 0 && (
          <Alert severity="warning" icon={<ErrorOutlineIcon fontSize="inherit" />}>
            <Typography variant="body2" sx={{ fontWeight: 600, mb: 0.5 }}>
              {MISSING_NORM_GROUPS.length} направления работ без активной нормы — расчёты идут без нормативного
              контроля
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 0.5 }}>
              {MISSING_NORM_GROUPS.map((g) => (
                <Chip key={g} size="small" label={g} variant="outlined" color="warning" />
              ))}
            </Stack>
          </Alert>
        )}

        {/* KPI-панель */}
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          {kpiCards.map((card) => {
            const active = kpiFilter === card.key;
            return (
              <Paper
                key={card.key}
                component="button"
                type="button"
                variant="outlined"
                onClick={() => setKpiFilter(active ? 'all' : card.key)}
                aria-pressed={active}
                sx={{
                  flex: '1 1 170px',
                  minWidth: 160,
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
            Показаны только строки: {kpiCards.find((c) => c.key === kpiFilter)?.label}. Нажмите на карточку ещё раз
            или на крестик, чтобы вернуться к полному списку.
          </Alert>
        )}

        {/* Таймлайн действия норм */}
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
            <Typography variant="subtitle1" component="h3">
              Таймлайн действия норм по группам IIC
            </Typography>
            <Stack direction="row" spacing={2} alignItems="center">
              {(['Error', 'Warning', 'Info', 'None'] as const).map((s) => (
                <Stack key={s} direction="row" spacing={0.5} alignItems="center">
                  <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: SEVERITY_COLOR[s] }} />
                  <Typography variant="caption" color="text.secondary">
                    {SEVERITY_LABEL[s]}
                  </Typography>
                </Stack>
              ))}
              <Stack direction="row" spacing={0.5} alignItems="center">
                <Box sx={{ width: 2, height: 12, bgcolor: 'text.primary' }} />
                <Typography variant="caption" color="text.secondary">
                  Сегодня
                </Typography>
              </Stack>
            </Stack>
          </Stack>
          <Box sx={{ mt: 2, position: 'relative' }}>
            <Box
              sx={{
                position: 'absolute',
                left: `${todayLeftPct}%`,
                top: 0,
                bottom: 0,
                width: '2px',
                bgcolor: 'text.primary',
                opacity: 0.6,
                zIndex: 2,
              }}
            />
            {sorted.map((n) => {
              const fromMs = new Date(n.effectiveFrom).getTime();
              const toMs = n.effectiveTo ? new Date(n.effectiveTo).getTime() : timelineMaxMs;
              const left = ((fromMs - timelineMinMs) / timelineSpan) * 100;
              const width = Math.max(1, ((toMs - fromMs) / timelineSpan) * 100);
              const sevKey = severityKey(n.issueSeverity);
              const isSelected = n.id === selectedId;
              return (
                <Box key={n.id} sx={{ mb: 1.25 }}>
                  <Typography
                    variant="caption"
                    sx={{ fontWeight: isSelected ? 700 : 600, display: 'block', mb: 0.25 }}
                  >
                    {n.groupName}
                  </Typography>
                  <Tooltip
                    title={
                      n.issueMessage
                        ? `${SEVERITY_LABEL[sevKey]}: ${n.issueMessage}`
                        : `${STATUS_LABEL[n.status]}, без замечаний`
                    }
                  >
                    <Box sx={{ position: 'relative', height: 22, bgcolor: 'action.hover', borderRadius: 0.5 }}>
                      <Box
                        onClick={() => setSelectedId(n.id)}
                        sx={{
                          position: 'absolute',
                          left: `${left}%`,
                          width: `${width}%`,
                          top: sevKey === 'None' ? 3 : 2,
                          height: sevKey === 'None' ? 16 : 18,
                          bgcolor: SEVERITY_COLOR[sevKey],
                          opacity: n.status === 'Historical' ? 0.55 : 0.9,
                          border: isSelected ? '2px solid' : sevKey === 'Error' ? '2px solid #7f1d1d' : 'none',
                          borderColor: isSelected ? 'text.primary' : undefined,
                          borderRadius: 0.5,
                          cursor: 'pointer',
                          '&:hover': { opacity: 1, filter: 'brightness(1.08)' },
                        }}
                      />
                    </Box>
                  </Tooltip>
                </Box>
              );
            })}
          </Box>
        </Paper>

        {/* Реестр норм: заголовок + переключатель вида */}
        <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="subtitle1" component="h3">
            Реестр норм ({filtered.length} из {RAW_NORMS.length})
          </Typography>
          <ToggleButtonGroup
            size="small"
            exclusive
            value={viewMode}
            onChange={(_, v) => v && setViewMode(v)}
            aria-label="Режим отображения реестра"
          >
            <ToggleButton value="table" aria-label="Таблица">
              <TableRowsOutlinedIcon fontSize="small" sx={{ mr: 0.75 }} />
              Таблица
            </ToggleButton>
            <ToggleButton value="heatmap" aria-label="Тепловая карта">
              <GridViewOutlinedIcon fontSize="small" sx={{ mr: 0.75 }} />
              Тепловая карта
            </ToggleButton>
          </ToggleButtonGroup>
        </Stack>

        {viewMode === 'table' ? (
          <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 520 }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ minWidth: 210 }}>Группа IIC</TableCell>
                  <TableCell sx={{ minWidth: 170 }}>Серьёзность</TableCell>
                  <TableCell>Формула</TableCell>
                  <TableCell align="right">μ</TableCell>
                  <TableCell align="right">σ</TableCell>
                  <TableCell align="right">
                    <Tooltip title="Коэффициент вариации σ/μ — насколько велик разброс относительно среднего. Подсвечивается, если заметно выше медианы по всем нормам.">
                      <span>CV</span>
                    </Tooltip>
                  </TableCell>
                  <TableCell sx={{ minWidth: 120 }}>
                    <Tooltip title="Достоверность нормы: чем больше версий факта (N) и выше качество подгонки формулы (R²), тем надёжнее оценка.">
                      <span>Достоверность</span>
                    </Tooltip>
                  </TableCell>
                  <TableCell sx={{ minWidth: 90 }}>
                    <Tooltip title="Динамика отношения факт/норма за последние 6 периодов — направление дрейфа важнее единичного выхода за допуск.">
                      <span>Дрейф</span>
                    </Tooltip>
                  </TableCell>
                  <TableCell>Статус</TableCell>
                  <TableCell align="right">Ревизия</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filtered.map((n) => {
                  const cv = n.sigma / n.mean;
                  const sevKey = severityKey(n.issueSeverity);
                  const isSelected = n.id === selectedId;
                  const stale = n.status === 'Active' && n.lastReviewedMonthsAgo >= 12;
                  const confidence = confidenceOf(n);
                  return (
                    <TableRow
                      key={n.id}
                      hover
                      selected={isSelected}
                      onClick={() => setSelectedId(n.id)}
                      sx={{ cursor: 'pointer' }}
                    >
                      <TableCell sx={{ fontWeight: isSelected ? 600 : 400 }}>{n.groupName}</TableCell>
                      <TableCell>
                        <Stack direction="row" spacing={0.75} alignItems="center">
                          <Box
                            sx={{
                              width: 9,
                              height: 9,
                              borderRadius: '50%',
                              flexShrink: 0,
                              bgcolor: SEVERITY_COLOR[sevKey],
                            }}
                          />
                          <Typography variant="body2" noWrap>
                            {SEVERITY_LABEL[sevKey]}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>{n.formulaType}</TableCell>
                      <TableCell align="right">{n.mean.toFixed(3)}</TableCell>
                      <TableCell align="right">{n.sigma.toFixed(3)}</TableCell>
                      <TableCell align="right" sx={{ backgroundColor: cvHeatBackground(cv) }}>
                        {cv.toFixed(2)}
                      </TableCell>
                      <TableCell>
                        <Tooltip title={`N = ${n.sampleSize} версий факта, R² = ${n.rSquared.toFixed(2)}`}>
                          <Stack direction="row" spacing={0.75} alignItems="center">
                            <Box
                              sx={{
                                width: 8,
                                height: 8,
                                borderRadius: '50%',
                                flexShrink: 0,
                                bgcolor: CONFIDENCE_COLOR[confidence],
                              }}
                            />
                            <Typography variant="body2" noWrap>
                              {CONFIDENCE_LABEL[confidence]}
                            </Typography>
                          </Stack>
                        </Tooltip>
                      </TableCell>
                      <TableCell>
                        <Box sx={{ color: SEVERITY_COLOR[sevKey], lineHeight: 0 }}>
                          <Sparkline values={n.driftHistory} color={SEVERITY_COLOR[sevKey]} />
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={STATUS_LABEL[n.status]}
                          color={n.status === 'Active' ? 'success' : n.status === 'Future' ? 'info' : 'default'}
                          variant={n.status === 'Historical' ? 'outlined' : 'filled'}
                        />
                      </TableCell>
                      <TableCell align="right">
                        {stale ? (
                          <Tooltip title="Норма активна и не пересматривалась 12+ месяцев">
                            <Chip
                              size="small"
                              icon={<ScheduleOutlinedIcon fontSize="small" />}
                              label={`${n.lastReviewedMonthsAgo} мес.`}
                              sx={{
                                bgcolor: SEVERITY_BG.Warning,
                                color: SEVERITY_COLOR.Warning,
                                fontWeight: 600,
                              }}
                            />
                          </Tooltip>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            {n.lastReviewedMonthsAgo} мес.
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Stack spacing={2}>
            {grouped.map(([category, norms]) => (
              <Paper key={category} variant="outlined" sx={{ p: 2 }}>
                <Typography variant="subtitle2" sx={{ mb: 1.25 }}>
                  {category}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1.25, flexWrap: 'wrap' }}>
                  {norms.map((n) => {
                    const sevKey = severityKey(n.issueSeverity);
                    const confidence = confidenceOf(n);
                    const isSelected = n.id === selectedId;
                    return (
                      <Tooltip
                        key={n.id}
                        title={`${n.groupName} · ${SEVERITY_LABEL[sevKey]} · достоверность: ${CONFIDENCE_LABEL[confidence]}`}
                      >
                        <Paper
                          component="button"
                          type="button"
                          onClick={() => setSelectedId(n.id)}
                          sx={{
                            width: 132,
                            height: 84,
                            p: 1,
                            display: 'flex',
                            flexDirection: 'column',
                            justifyContent: 'space-between',
                            textAlign: 'left',
                            cursor: 'pointer',
                            font: 'inherit',
                            bgcolor: SEVERITY_BG[sevKey],
                            border: isSelected ? '2px solid' : '1px solid transparent',
                            borderColor: isSelected ? 'text.primary' : 'transparent',
                            '&:hover': { filter: 'brightness(0.97)' },
                          }}
                        >
                          <Typography variant="caption" sx={{ fontWeight: 600, lineHeight: 1.2 }} noWrap>
                            {n.groupName.split(' — ')[1] ?? n.groupName}
                          </Typography>
                          <Stack direction="row" justifyContent="space-between" alignItems="center">
                            <Box
                              sx={{
                                width: 8,
                                height: 8,
                                borderRadius: '50%',
                                bgcolor: SEVERITY_COLOR[sevKey],
                              }}
                            />
                            <Box
                              sx={{
                                width: 8,
                                height: 8,
                                borderRadius: '50%',
                                bgcolor: CONFIDENCE_COLOR[confidence],
                              }}
                            />
                            <Typography variant="caption" color="text.secondary">
                              CV {(n.sigma / n.mean).toFixed(2)}
                            </Typography>
                          </Stack>
                        </Paper>
                      </Tooltip>
                    );
                  })}
                </Box>
              </Paper>
            ))}
            <Stack direction="row" spacing={2} alignItems="center" sx={{ px: 1 }}>
              <Typography variant="caption" color="text.secondary">
                Цвет ячейки — серьёзность отклонения. Левая точка — серьёзность, правая — достоверность нормы.
              </Typography>
            </Stack>
          </Stack>
        )}

        {/* Панель деталей выбранной нормы */}
        {selected && simData && (
          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Stack direction="row" justifyContent="space-between" alignItems="flex-start" flexWrap="wrap" useFlexGap spacing={1}>
              <Box>
                <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                  {selected.groupName}
                </Typography>
                <Stack direction="row" spacing={1} sx={{ mt: 0.75 }} flexWrap="wrap" useFlexGap>
                  <Chip
                    size="small"
                    label={SEVERITY_LABEL[severityKey(selected.issueSeverity)]}
                    sx={{
                      bgcolor: SEVERITY_BG[severityKey(selected.issueSeverity)],
                      color: SEVERITY_COLOR[severityKey(selected.issueSeverity)],
                      fontWeight: 600,
                    }}
                  />
                  <Chip
                    size="small"
                    variant="outlined"
                    label={STATUS_LABEL[selected.status]}
                  />
                  <Chip size="small" variant="outlined" label={`Формула: ${selected.formulaType}`} />
                  <Tooltip title={`N = ${selected.sampleSize} версий факта, R² = ${selected.rSquared.toFixed(2)}`}>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={`Достоверность: ${CONFIDENCE_LABEL[confidenceOf(selected)]}`}
                      sx={{ borderColor: CONFIDENCE_COLOR[confidenceOf(selected)], color: CONFIDENCE_COLOR[confidenceOf(selected)] }}
                    />
                  </Tooltip>
                </Stack>
              </Box>
              <Button
                size="small"
                variant="outlined"
                startIcon={<CompareArrowsOutlinedIcon fontSize="small" />}
                href="/dev/fact-norm-mockup"
              >
                Показать в сравнении факта и норм
              </Button>
            </Stack>

            {selected.issueMessage && (
              <Alert
                severity={
                  severityKey(selected.issueSeverity) === 'Error'
                    ? 'error'
                    : severityKey(selected.issueSeverity) === 'Warning'
                      ? 'warning'
                      : 'info'
                }
                sx={{ mt: 2 }}
              >
                <Typography variant="body2">{selected.issueMessage}</Typography>
                {selected.issueRecommendation && (
                  <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 600 }}>
                    Рекомендация: {selected.issueRecommendation}
                  </Typography>
                )}
              </Alert>
            )}

            <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mt: 2 }}>
              {selected.previousMean != null && selected.previousSigma != null && (
                <Box sx={{ minWidth: 260, flex: '1 1 260px' }}>
                  <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>
                    Сравнение с предыдущей версией
                  </Typography>
                  <Stack spacing={0.75}>
                    <Stack direction="row" justifyContent="space-between">
                      <Typography variant="caption" color="text.secondary">
                        μ: {selected.previousMean.toFixed(3)} → {selected.mean.toFixed(3)}
                      </Typography>
                      <Typography
                        variant="caption"
                        sx={{ fontWeight: 600 }}
                        color={selected.mean > selected.previousMean ? 'text.primary' : 'text.secondary'}
                      >
                        {selected.mean >= selected.previousMean ? '+' : ''}
                        {(((selected.mean - selected.previousMean) / selected.previousMean) * 100).toFixed(1)}%
                      </Typography>
                    </Stack>
                    <Stack direction="row" justifyContent="space-between">
                      <Typography variant="caption" color="text.secondary">
                        σ: {selected.previousSigma.toFixed(3)} → {selected.sigma.toFixed(3)}
                      </Typography>
                      <Typography
                        variant="caption"
                        sx={{ fontWeight: 600 }}
                        color={selected.sigma > selected.previousSigma * 1.2 ? 'error.main' : 'text.secondary'}
                      >
                        {selected.sigma >= selected.previousSigma ? '+' : ''}
                        {(((selected.sigma - selected.previousSigma) / selected.previousSigma) * 100).toFixed(1)}%
                      </Typography>
                    </Stack>
                    <Typography variant="caption" color="text.secondary">
                      Действует с {formatRuDate(selected.effectiveFrom)}
                      {selected.effectiveTo ? ` по ${formatRuDate(selected.effectiveTo)}` : ' (текущая)'}
                    </Typography>
                  </Stack>
                </Box>
              )}

              <Box sx={{ minWidth: 320, flex: '2 1 420px' }}>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                  Коридор допуска (с учётом симулятора)
                </Typography>
                <NormBandChart band={simData.proposedBand} deviationRatio={simData.newRatio} />
              </Box>
            </Box>

            {/* Симулятор «что если» */}
            <Paper variant="outlined" sx={{ p: 2, mt: 2, bgcolor: 'action.hover' }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  Симулятор «что если»: правка параметров формулы
                </Typography>
                <Button
                  size="small"
                  startIcon={<RestartAltOutlinedIcon fontSize="small" />}
                  onClick={() => {
                    setMeanOffsetPct(0);
                    setSigmaMultiplier(1);
                  }}
                  disabled={meanOffsetPct === 0 && sigmaMultiplier === 1}
                >
                  Сбросить
                </Button>
              </Stack>
              <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap', mt: 1.5 }}>
                <Box sx={{ minWidth: 220, flex: '1 1 220px' }}>
                  <Typography variant="caption" color="text.secondary">
                    Смещение μ: {meanOffsetPct > 0 ? '+' : ''}
                    {meanOffsetPct}% (было {selected.mean.toFixed(3)}, станет {simData.newMean.toFixed(3)})
                  </Typography>
                  <Slider
                    size="small"
                    min={-20}
                    max={20}
                    step={1}
                    value={meanOffsetPct}
                    onChange={(_, v) => setMeanOffsetPct(v as number)}
                    aria-label="Смещение μ, %"
                  />
                </Box>
                <Box sx={{ minWidth: 220, flex: '1 1 220px' }}>
                  <Typography variant="caption" color="text.secondary">
                    Ширина допуска (×σ): {sigmaMultiplier.toFixed(2)}× (было ±
                    {((selected.sigma / selected.mean) * 100).toFixed(0)}%, станет ±
                    {(simData.newRatio * 100).toFixed(0)}%)
                  </Typography>
                  <Slider
                    size="small"
                    min={0.5}
                    max={2}
                    step={0.05}
                    value={sigmaMultiplier}
                    onChange={(_, v) => setSigmaMultiplier(v as number)}
                    aria-label="Множитель ширины допуска"
                  />
                </Box>
              </Box>
              <Stack direction="row" spacing={3} sx={{ mt: 2 }} flexWrap="wrap" useFlexGap>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Точек факта в допуске сейчас
                  </Typography>
                  <Typography variant="h6" sx={{ lineHeight: 1.2 }}>
                    {simData.beforePct}%
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Точек факта в допуске после правки
                  </Typography>
                  <Typography
                    variant="h6"
                    sx={{ lineHeight: 1.2 }}
                    color={simData.afterPct > simData.beforePct ? 'success.main' : simData.afterPct < simData.beforePct ? 'error.main' : 'text.primary'}
                  >
                    {simData.afterPct}%
                    {simData.afterPct !== simData.beforePct && (
                      <Typography component="span" variant="caption" sx={{ ml: 0.75 }}>
                        ({simData.afterPct > simData.beforePct ? '+' : ''}
                        {simData.afterPct - simData.beforePct} п.п.)
                      </Typography>
                    )}
                  </Typography>
                </Box>
              </Stack>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                Расчёт основан на исторических точках факта по этой группе IIC и не публикует новую версию нормы —
                только предварительный просчёт эффекта.
              </Typography>
            </Paper>
          </Paper>
        )}
      </Box>
    </Box>
  );
}
