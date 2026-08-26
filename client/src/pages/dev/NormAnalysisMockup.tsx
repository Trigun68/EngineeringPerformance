import { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
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
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import ShowChartOutlinedIcon from '@mui/icons-material/ShowChartOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined';
import { NormBandChart } from '../../features/norms/NormBandChart';
import type { NormPreviewPoint } from '../../api/normsApi';

type IssueSeverity = 'Error' | 'Warning' | 'Info' | null;
type NormStatus = 'Active' | 'Historical' | 'Future';

interface MockNorm {
  id: number;
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

const STATUS_COLOR: Record<NormStatus, 'success' | 'default' | 'info'> = {
  Active: 'success',
  Historical: 'default',
  Future: 'info',
};

function severityKey(s: IssueSeverity): 'Error' | 'Warning' | 'Info' | 'None' {
  return s ?? 'None';
}

const RAW_NORMS: MockNorm[] = [
  {
    id: 101,
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
  },
  {
    id: 102,
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
  },
  {
    id: 103,
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
  },
  {
    id: 104,
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
  },
  {
    id: 105,
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
  },
  {
    id: 106,
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
  },
  {
    id: 107,
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
  },
  {
    id: 108,
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
  },
  {
    id: 109,
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
  },
  {
    id: 110,
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
  },
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

function formatRuDate(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}.${m}.${y}`;
}

type KpiFilter = 'all' | 'Error' | 'Warning' | 'cv' | 'ok';

export default function NormAnalysisMockup() {
  const [kpiFilter, setKpiFilter] = useState<KpiFilter>('all');
  const [selectedId, setSelectedId] = useState<number | null>(105);

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
    return { total, errors, warnings, cvOutliers, ok };
  }, []);

  const filtered = useMemo(() => {
    if (kpiFilter === 'all') return sorted;
    if (kpiFilter === 'ok') return sorted.filter((n) => n.issueSeverity === null);
    if (kpiFilter === 'cv') return sorted.filter((n) => n.sigma / n.mean >= MEDIAN_CV * 1.5);
    return sorted.filter((n) => n.issueSeverity === kpiFilter);
  }, [sorted, kpiFilter]);

  const selected = RAW_NORMS.find((n) => n.id === selectedId) ?? null;

  const kpiCards: Array<{
    key: KpiFilter;
    label: string;
    value: number;
    icon: React.ReactNode;
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

  return (
    <Box sx={{ height: '100%', overflowY: 'auto', bgcolor: 'background.default' }}>
      <Box sx={{ p: 3, display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 1280, mx: 'auto' }}>
        <Box>
          <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
            Анализ норм
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Мокап переработанного дизайна: кликабельная сводка health-check, коэффициент вариации (CV) с тепловой
            подсветкой аномального разброса, единая шкала серьёзности вместо счётчика проблем, таймлайн с легендой
            и меткой «сегодня», сравнение параметров нормы с предыдущей версией.
          </Typography>
        </Box>

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
                  flex: '1 1 200px',
                  minWidth: 190,
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

        {/* Реестр норм */}
        <Typography variant="subtitle1" component="h3">
          Реестр норм ({filtered.length} из {RAW_NORMS.length})
        </Typography>
        <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 520 }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell sx={{ minWidth: 220 }}>Группа IIC</TableCell>
                <TableCell sx={{ minWidth: 170 }}>Серьёзность</TableCell>
                <TableCell>Формула</TableCell>
                <TableCell align="right">μ</TableCell>
                <TableCell align="right">σ</TableCell>
                <TableCell align="right">
                  <Tooltip title="Коэффициент вариации σ/μ — насколько велик разброс относительно среднего. Подсвечивается, если заметно выше медианы по всем нормам.">
                    <span>CV</span>
                  </Tooltip>
                </TableCell>
                <TableCell>Статус</TableCell>
                <TableCell>Период действия</TableCell>
                <TableCell align="right">Ревизия</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filtered.map((n) => {
                const cv = n.sigma / n.mean;
                const sevKey = severityKey(n.issueSeverity);
                const isSelected = n.id === selectedId;
                const stale = n.status === 'Active' && n.lastReviewedMonthsAgo >= 12;
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
                      <Chip size="small" color={STATUS_COLOR[n.status]} label={STATUS_LABEL[n.status]} />
                    </TableCell>
                    <TableCell>
                      {formatRuDate(n.effectiveFrom)} — {n.effectiveTo ? formatRuDate(n.effectiveTo) : '∞'}
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

        {/* Панель деталей выбранной нормы */}
        {selected && (
          <Paper variant="outlined" sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="subtitle1" component="h3" sx={{ mr: 1 }}>
                {selected.groupName}
              </Typography>
              <Chip
                size="small"
                sx={{
                  bgcolor: SEVERITY_BG[severityKey(selected.issueSeverity)],
                  color: SEVERITY_COLOR[severityKey(selected.issueSeverity)],
                  fontWeight: 600,
                }}
                label={SEVERITY_LABEL[severityKey(selected.issueSeverity)]}
              />
              <Chip size="small" color={STATUS_COLOR[selected.status]} label={STATUS_LABEL[selected.status]} />
              <Chip size="small" variant="outlined" label={selected.formulaType} />
            </Stack>

            {selected.issueMessage && (
              <Alert
                severity={
                  selected.issueSeverity === 'Error'
                    ? 'error'
                    : selected.issueSeverity === 'Warning'
                      ? 'warning'
                      : 'info'
                }
                sx={{ py: 0.5 }}
              >
                {selected.issueMessage}
                {selected.issueRecommendation ? (
                  <>
                    <br />
                    <strong>Что делать:</strong> {selected.issueRecommendation}
                  </>
                ) : null}
              </Alert>
            )}

            <Stack direction="row" spacing={3} flexWrap="wrap" useFlexGap>
              <Box>
                <Typography variant="caption" color="text.secondary" display="block">
                  μ (текущая → предыдущая версия)
                </Typography>
                <Typography variant="body1">
                  {selected.mean.toFixed(3)}
                  {selected.previousMean != null ? (
                    <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.75 }}>
                      (было {selected.previousMean.toFixed(3)},{' '}
                      {selected.mean >= selected.previousMean ? '+' : ''}
                      {(((selected.mean - selected.previousMean) / selected.previousMean) * 100).toFixed(0)}%)
                    </Typography>
                  ) : (
                    <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.75 }}>
                      (первая версия)
                    </Typography>
                  )}
                </Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary" display="block">
                  σ (текущая → предыдущая версия)
                </Typography>
                <Typography
                  variant="body1"
                  sx={{
                    color:
                      selected.previousSigma != null && selected.sigma > selected.previousSigma * 1.3
                        ? SEVERITY_COLOR.Error
                        : selected.previousSigma != null && selected.sigma < selected.previousSigma * 0.85
                          ? SEVERITY_COLOR.None
                          : 'text.primary',
                    fontWeight:
                      selected.previousSigma != null && selected.sigma > selected.previousSigma * 1.3 ? 700 : 400,
                  }}
                >
                  {selected.sigma.toFixed(3)}
                  {selected.previousSigma != null ? (
                    <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.75 }}>
                      (было {selected.previousSigma.toFixed(3)},{' '}
                      {selected.sigma >= selected.previousSigma ? '+' : ''}
                      {(((selected.sigma - selected.previousSigma) / selected.previousSigma) * 100).toFixed(0)}%)
                    </Typography>
                  ) : (
                    <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.75 }}>
                      (первая версия)
                    </Typography>
                  )}
                </Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary" display="block">
                  Коэффициент вариации (CV)
                </Typography>
                <Typography
                  variant="body1"
                  sx={{
                    color:
                      selected.sigma / selected.mean >= MEDIAN_CV * 1.5 ? SEVERITY_COLOR.Warning : 'text.primary',
                    fontWeight: selected.sigma / selected.mean >= MEDIAN_CV * 1.5 ? 700 : 400,
                  }}
                >
                  {(selected.sigma / selected.mean).toFixed(2)}
                  <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.75 }}>
                    (медиана по реестру: {MEDIAN_CV.toFixed(2)})
                  </Typography>
                </Typography>
              </Box>
            </Stack>

            <NormBandChart
              band={generateBand(selected.mean, Math.min(0.6, selected.sigma / selected.mean))}
              deviationRatio={Math.min(0.6, selected.sigma / selected.mean)}
              chartVisible
            />
          </Paper>
        )}
      </Box>
    </Box>
  );
}
