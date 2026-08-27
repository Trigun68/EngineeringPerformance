import { useMemo, type ReactNode } from 'react';
import { Alert, Box, Chip, Divider, Paper, Stack, Typography } from '@mui/material';
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import { FactPlanToggle, type FactPlanMode } from './FactPlanToggle';
import { KpiSummaryRow, type KpiItem } from './KpiSummaryRow';
import { PeriodDynamicsChart, type DynamicsSeries } from './PeriodDynamicsChart';
import { FactorWaterfallChart } from './FactorWaterfallChart';
import {
  bucketPointsByMonth,
  buildWaterfallSteps,
  computePeriodKpi,
  type GroupTrendLike,
  type TrendPointLike,
  type TrendVersionRow,
} from './trendDetailCalc';

/**
 * Общий каркас детализированных тренд-вкладок аналитики (дизайн TrendDetailMockup):
 * переключатель Факт/План, KPI-полоса, динамика по месяцам, водопад факторов,
 * таблица версий выборки. Данные и пороги — реальные, из API.
 */

export interface TrendDetailMetricConfig {
  yAxisTitle: string;
  /** Суффикс единицы измерения ('' для Z, ' %' для индекса). */
  unit: string;
  digits: number;
  /** Опорная линия y=0 (уместно для Z-оценки). */
  zeroLine: boolean;
  /** Какой знак изменения — улучшение (для Z меньше = лучше → 'negative'). */
  goodDirection: 'positive' | 'negative';
  factColor: string;
  planColor: string;
}

interface Props {
  mode: FactPlanMode;
  onModeChange: (mode: FactPlanMode) => void;
  config: TrendDetailMetricConfig;
  /** Подпись чипа периода (уже отформатированная). */
  periodLabel: string;
  planPoints: TrendPointLike[];
  factPoints: TrendPointLike[];
  planVersions: TrendVersionRow[];
  factVersions: TrendVersionRow[];
  /** Группы факта для водопада «Разбор изменения по факторам». */
  waterfallGroups: GroupTrendLike[];
  /** Дополнительные чипы/контролы рядом с чипом периода (например, режим взвешивания). */
  headerExtras?: ReactNode;
  testId: string;
}

function formatSigned(value: number, digits: number, unit: string): string {
  const sign = value > 0 ? '+' : '';
  return `${sign}${value.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits })}${unit}`;
}

function formatPlain(value: number, digits: number, unit: string): string {
  return `${value.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits })}${unit}`;
}

function formatOrDash(value: number | null, digits: number, unit: string): string {
  return value === null ? '—' : formatPlain(value, digits, unit);
}

function VersionsTable({
  rows,
  unit,
  digits,
  testId,
}: {
  rows: TrendVersionRow[];
  unit: string;
  digits: number;
  testId: string;
}) {
  return (
    <Paper variant="outlined" sx={{ overflow: 'hidden' }} data-testid={testId}>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: '1fr 1fr 1fr auto',
          gap: 1,
          px: 2,
          py: 1,
          bgcolor: 'action.hover',
        }}
      >
        <Typography variant="caption" color="text.secondary">
          Проект
        </Typography>
        <Typography variant="caption" color="text.secondary">
          Версия
        </Typography>
        <Typography variant="caption" color="text.secondary">
          Дата
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'right' }}>
          Значение
        </Typography>
      </Box>
      <Divider />
      <Stack divider={<Divider />}>
        {rows.map((row, i) => (
          <Box
            key={`${row.projectCode}-${row.versionName}-${i}`}
            sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr auto', gap: 1, px: 2, py: 1 }}
          >
            <Typography variant="body2">{row.projectCode}</Typography>
            <Typography variant="body2" color="text.secondary">
              {row.versionName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {new Date(row.date).toLocaleDateString('ru-RU')}
            </Typography>
            <Typography variant="body2" sx={{ textAlign: 'right', fontWeight: 600 }}>
              {formatOrDash(row.value, digits, unit)}
            </Typography>
          </Box>
        ))}
      </Stack>
    </Paper>
  );
}

export function TrendDetailView({
  mode,
  onModeChange,
  config,
  periodLabel,
  planPoints,
  factPoints,
  planVersions,
  factVersions,
  waterfallGroups,
  headerExtras,
  testId,
}: Props) {
  const planBuckets = useMemo(() => bucketPointsByMonth(planPoints), [planPoints]);
  const factBuckets = useMemo(() => bucketPointsByMonth(factPoints), [factPoints]);

  const kpiItems = useMemo<KpiItem[]>(() => {
    const primary = mode === 'plan' ? planBuckets : factBuckets;
    const secondary = mode === 'plan' ? factBuckets : planBuckets;
    const kpi = computePeriodKpi(primary);
    const secondaryKpi = computePeriodKpi(secondary);
    const versions = mode === 'plan' ? planVersions : factVersions;
    const projectCount = new Set(versions.map((v) => v.projectCode)).size;

    const tone =
      kpi.delta === null || kpi.delta === 0
        ? 'neutral'
        : kpi.delta > 0 === (config.goodDirection === 'positive')
          ? 'positive'
          : 'negative';
    const otherLabel = mode === 'plan' ? 'факт' : 'план';

    return [
      {
        key: 'current',
        label: `Средняя сейчас (${mode === 'plan' ? 'план' : 'факт'})`,
        value: formatOrDash(kpi.current, config.digits, config.unit),
        helperText:
          mode === 'both'
            ? `${otherLabel}: ${formatOrDash(secondaryKpi.current, config.digits, config.unit)}`
            : undefined,
        tone: 'neutral',
      },
      {
        key: 'initial',
        label: 'Было на начало периода',
        value: formatOrDash(kpi.initial, config.digits, config.unit),
        tone: 'neutral',
      },
      {
        key: 'delta',
        label: 'Изменение за период',
        value: kpi.delta === null ? '—' : formatSigned(kpi.delta, config.digits, config.unit),
        tone,
        helperText:
          kpi.delta === null || kpi.delta === 0
            ? 'Без изменения'
            : tone === 'positive'
              ? 'Улучшение'
              : 'Ухудшение',
      },
      {
        key: 'coverage',
        label: 'Версий в выборке',
        value: String(versions.length),
        helperText: `${projectCount} проектов, основные версии (isPrimary)`,
        tone: 'neutral',
      },
    ];
  }, [mode, planBuckets, factBuckets, planVersions, factVersions, config]);

  const dynamicsSeries = useMemo<DynamicsSeries[]>(() => {
    const series: DynamicsSeries[] = [];
    if (mode === 'fact' || mode === 'both') {
      series.push({
        id: 'fact',
        name: 'Факт (основные версии)',
        color: config.factColor,
        points: factBuckets,
      });
    }
    if (mode === 'plan' || mode === 'both') {
      series.push({
        id: 'plan',
        name: 'План (основные версии)',
        color: config.planColor,
        dashStyle: 'Dash',
        points: planBuckets,
      });
    }
    return series;
  }, [mode, factBuckets, planBuckets, config]);

  const waterfall = useMemo(() => {
    if (mode === 'plan') return null;
    const kpi = computePeriodKpi(factBuckets);
    const steps = buildWaterfallSteps(waterfallGroups, kpi.delta);
    if (!steps || kpi.initial === null) return null;
    return { steps, startValue: kpi.initial };
  }, [mode, factBuckets, waterfallGroups]);

  const versions = mode === 'plan' ? planVersions : factVersions;

  return (
    <Stack spacing={2.5} data-testid={testId}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 1.5,
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Chip
            icon={<CalendarMonthOutlinedIcon fontSize="small" />}
            label={periodLabel}
            variant="outlined"
            data-testid={`${testId}-period-chip`}
          />
          {headerExtras}
        </Stack>
        <FactPlanToggle value={mode} onChange={onModeChange} testId={`${testId}-fact-plan-toggle`} />
      </Box>

      <KpiSummaryRow items={kpiItems} testId={`${testId}-kpi`} />

      <Typography variant="h6" component="h2">
        Динамика по периодам
      </Typography>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <PeriodDynamicsChart
          series={dynamicsSeries}
          title={undefined}
          yAxisTitle={config.yAxisTitle}
          zeroLine={config.zeroLine}
          valueSuffix={config.unit}
          valueDigits={config.digits}
          testId={`${testId}-dynamics-chart`}
        />
      </Paper>

      {mode !== 'plan' ? (
        waterfall ? (
          <>
            <Typography variant="h6" component="h2">
              Разбор изменения по факторам
            </Typography>
            <Paper variant="outlined" sx={{ p: 2 }}>
              <FactorWaterfallChart
                startLabel="Было"
                startValue={waterfall.startValue}
                endLabel="Стало"
                steps={waterfall.steps}
                goodDirection={config.goodDirection}
                valueFormatter={(v) => formatSigned(v, config.digits, config.unit)}
                testId={`${testId}-waterfall`}
              />
            </Paper>
          </>
        ) : (
          <Alert severity="info" data-testid={`${testId}-waterfall-empty`}>
            Недостаточно данных по направлениям для разбора изменения за период.
          </Alert>
        )
      ) : (
        <Alert severity="info" data-testid={`${testId}-waterfall-plan-note`}>
          Разбор по факторам показывается для факта — план по конструкции близок к норме и почти не
          меняется в течение периода.
        </Alert>
      )}

      <Typography variant="h6" component="h2">
        Версии в выборке периода
      </Typography>
      {versions.length > 0 ? (
        <VersionsTable
          rows={versions}
          unit={config.unit}
          digits={config.digits}
          testId={`${testId}-versions-table`}
        />
      ) : (
        <Alert severity="info" data-testid={`${testId}-versions-empty`}>
          Нет основных версий в выбранном периоде.
        </Alert>
      )}
    </Stack>
  );
}
