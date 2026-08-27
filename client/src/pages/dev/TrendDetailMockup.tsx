import { useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
  Container,
  Divider,
  Grid,
  Paper,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import { FactPlanToggle, type FactPlanMode } from '../../features/analytics/shared/FactPlanToggle';
import { KpiSummaryRow, type KpiItem } from '../../features/analytics/shared/KpiSummaryRow';
import {
  PeriodDynamicsChart,
  type DynamicsSeries,
} from '../../features/analytics/shared/PeriodDynamicsChart';
import {
  FactorWaterfallChart,
  type WaterfallStep,
} from '../../features/analytics/shared/FactorWaterfallChart';

// -----------------------------------------------------------------------------
// Дев-мокап: детализированные вкладки «Тренд Z-оценок» и «Тренд производительности».
// Демонстрирует предложенный набор виджетов на статичных демо-данных — до
// проектирования backend-агрегации (бакеты по под-периодам, разбор по факторам).
// Доступно без авторизации и без AppLayout — только для просмотра дизайна.
// -----------------------------------------------------------------------------

type MockTab = 'z-score' | 'productivity';

const MONTH_DATES = ['2026-05-01', '2026-06-01', '2026-07-01', '2026-08-01'];

interface MetricConfig {
  tabLabel: string;
  pageTitle: string;
  pageSubtitle: string;
  unit: string;
  digits: number;
  zeroLine: boolean;
  goodDirection: 'positive' | 'negative';
  fact: number[];
  plan: number[];
  waterfallSteps: WaterfallStep[];
  versionsFact: { project: string; version: string; date: string; value: number }[];
  versionsPlan: { project: string; version: string; date: string; value: number }[];
  factColor: string;
  planColor: string;
  chartYAxisTitle: string;
}

function formatSigned(value: number, digits: number, unit: string): string {
  const sign = value > 0 ? '+' : '';
  return `${sign}${value.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits })}${unit}`;
}

function formatPlain(value: number, digits: number, unit: string): string {
  return `${value.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits })}${unit}`;
}

const CONFIGS: Record<MockTab, MetricConfig> = {
  'z-score': {
    tabLabel: 'Тренд Z-оценок',
    pageTitle: 'Тренд Z-оценок',
    pageSubtitle:
      'Динамика средневзвешенной Z-оценки по основным версиям проектов за выбранный период — насколько факт и план отклоняются от действующих норм IIC.',
    unit: '',
    digits: 2,
    zeroLine: true,
    goodDirection: 'positive',
    fact: [-0.42, -0.31, -0.12, 0.05],
    plan: [0.01, 0.02, 0.01, 0.03],
    waterfallSteps: [
      { name: 'Металлоконструкции', value: 0.22 },
      { name: 'Электромонтажные работы', value: 0.15 },
      { name: 'Трубопроводы', value: 0.05 },
      { name: 'Прочие направления', value: 0.05 },
    ],
    versionsFact: [
      { project: 'ПР-014', version: 'Факт v6', date: '2026-08-12', value: 0.31 },
      { project: 'ПР-021', version: 'Факт v4', date: '2026-08-05', value: 0.18 },
      { project: 'ПР-009', version: 'Факт v9', date: '2026-07-28', value: -0.05 },
      { project: 'ПР-033', version: 'Факт v2', date: '2026-07-14', value: -0.21 },
      { project: 'ПР-014', version: 'Факт v5', date: '2026-06-20', value: -0.38 },
      { project: 'ПР-021', version: 'Факт v3', date: '2026-05-30', value: -0.55 },
    ],
    versionsPlan: [
      { project: 'ПР-014', version: 'План v2', date: '2026-08-01', value: 0.04 },
      { project: 'ПР-021', version: 'План v2', date: '2026-07-01', value: 0.02 },
      { project: 'ПР-009', version: 'План v3', date: '2026-06-01', value: 0.01 },
      { project: 'ПР-033', version: 'План v1', date: '2026-05-01', value: 0.00 },
    ],
    factColor: '#8E44AD',
    planColor: '#2E86C1',
    chartYAxisTitle: 'Средневзвешенная Z-оценка',
  },
  productivity: {
    tabLabel: 'Тренд производительности',
    pageTitle: 'Тренд производительности',
    pageSubtitle:
      'Соотношение факта и плана с нормативной выработкой по основным версиям проектов — на сколько процентов быстрее или медленнее нормы выполняется работа.',
    unit: ' %',
    digits: 1,
    zeroLine: false,
    goodDirection: 'positive',
    fact: [88, 91, 95, 97],
    plan: [98, 99, 100, 100],
    waterfallSteps: [
      { name: 'Металлоконструкции', value: 5 },
      { name: 'Электромонтажные работы', value: 3 },
      { name: 'Трубопроводы', value: -1 },
      { name: 'Прочие направления', value: 2 },
    ],
    versionsFact: [
      { project: 'ПР-014', version: 'Факт v6', date: '2026-08-12', value: 101 },
      { project: 'ПР-021', version: 'Факт v4', date: '2026-08-05', value: 98 },
      { project: 'ПР-009', version: 'Факт v9', date: '2026-07-28', value: 92 },
      { project: 'ПР-033', version: 'Факт v2', date: '2026-07-14', value: 87 },
      { project: 'ПР-014', version: 'Факт v5', date: '2026-06-20', value: 84 },
      { project: 'ПР-021', version: 'Факт v3', date: '2026-05-30', value: 79 },
    ],
    versionsPlan: [
      { project: 'ПР-014', version: 'План v2', date: '2026-08-01', value: 100 },
      { project: 'ПР-021', version: 'План v2', date: '2026-07-01', value: 100 },
      { project: 'ПР-009', version: 'План v3', date: '2026-06-01', value: 99 },
      { project: 'ПР-033', version: 'План v1', date: '2026-05-01', value: 97 },
    ],
    factColor: '#C77B2B',
    planColor: '#2E86C1',
    chartYAxisTitle: 'Индекс производительности, % к норме',
  },
};

function buildDynamicsSeries(config: MetricConfig, mode: FactPlanMode): DynamicsSeries[] {
  const series: DynamicsSeries[] = [];
  if (mode === 'fact' || mode === 'both') {
    series.push({
      id: 'fact',
      name: 'Факт (основные версии)',
      color: config.factColor,
      points: MONTH_DATES.map((date, i) => ({ date, value: config.fact[i] })),
    });
  }
  if (mode === 'plan' || mode === 'both') {
    series.push({
      id: 'plan',
      name: 'План (основные версии)',
      color: config.planColor,
      dashStyle: 'Dash',
      points: MONTH_DATES.map((date, i) => ({ date, value: config.plan[i] })),
    });
  }
  return series;
}

function useKpiItems(config: MetricConfig, mode: FactPlanMode): KpiItem[] {
  return useMemo(() => {
    const series = mode === 'plan' ? config.plan : config.fact;
    const other = mode === 'plan' ? config.fact : config.plan;
    const current = series[series.length - 1];
    const initial = series[0];
    const delta = current - initial;
    const tone = delta === 0 ? 'neutral' : (delta > 0) === (config.goodDirection === 'positive') ? 'positive' : 'negative';
    const otherLabel = mode === 'plan' ? 'факт' : 'план';
    const otherCurrent = other[other.length - 1];
    const versions = mode === 'plan' ? config.versionsPlan : config.versionsFact;
    const projectCount = new Set(versions.map((v) => v.project)).size;

    return [
      {
        key: 'current',
        label: `Средняя сейчас (${mode === 'plan' ? 'план' : 'факт'})`,
        value: formatPlain(current, config.digits, config.unit),
        helperText: mode === 'both' ? `${otherLabel}: ${formatPlain(otherCurrent, config.digits, config.unit)}` : undefined,
        tone: 'neutral',
      },
      {
        key: 'initial',
        label: 'Было на начало периода',
        value: formatPlain(initial, config.digits, config.unit),
        tone: 'neutral',
      },
      {
        key: 'delta',
        label: 'Изменение за период',
        value: formatSigned(delta, config.digits, config.unit),
        tone,
        helperText: delta === 0 ? 'Без изменения' : tone === 'positive' ? 'Улучшение' : 'Ухудшение',
      },
      {
        key: 'coverage',
        label: 'Версий в выборке',
        value: String(versions.length),
        helperText: `${projectCount} проектов, основные версии (isPrimary)`,
        tone: 'neutral',
      },
    ];
  }, [config, mode]);
}

function VersionsTable({
  rows,
  unit,
  digits,
}: {
  rows: { project: string; version: string; date: string; value: number }[];
  unit: string;
  digits: number;
}) {
  return (
    <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
      <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr auto', gap: 1, px: 2, py: 1, bgcolor: 'action.hover' }}>
        <Typography variant="caption" color="text.secondary">Проект</Typography>
        <Typography variant="caption" color="text.secondary">Версия</Typography>
        <Typography variant="caption" color="text.secondary">Дата</Typography>
        <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'right' }}>Значение</Typography>
      </Box>
      <Divider />
      <Stack divider={<Divider />}>
        {rows.map((row, i) => (
          <Box
            key={`${row.project}-${row.version}-${i}`}
            sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr auto', gap: 1, px: 2, py: 1 }}
          >
            <Typography variant="body2">{row.project}</Typography>
            <Typography variant="body2" color="text.secondary">{row.version}</Typography>
            <Typography variant="body2" color="text.secondary">
              {new Date(row.date).toLocaleDateString('ru-RU')}
            </Typography>
            <Typography variant="body2" sx={{ textAlign: 'right', fontWeight: 600 }}>
              {formatPlain(row.value, digits, unit)}
            </Typography>
          </Box>
        ))}
      </Stack>
    </Paper>
  );
}

function MetricTabContent({ config }: { config: MetricConfig }) {
  const [mode, setMode] = useState<FactPlanMode>('fact');
  const kpiItems = useKpiItems(config, mode);
  const dynamicsSeries = useMemo(() => buildDynamicsSeries(config, mode), [config, mode]);
  const versions = mode === 'plan' ? config.versionsPlan : config.versionsFact;

  return (
    <Stack spacing={2.5}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 1.5,
        }}
      >
        <Chip
          icon={<CalendarMonthOutlinedIcon fontSize="small" />}
          label="Период: 01.05.2026 – 26.08.2026 (последние 4 месяца)"
          variant="outlined"
          data-testid="trend-detail-period-chip"
        />
        <FactPlanToggle value={mode} onChange={setMode} testId="trend-detail-fact-plan-toggle" />
      </Box>

      <KpiSummaryRow items={kpiItems} testId="trend-detail-kpi" />

      <Typography variant="h6" component="h2">
        Динамика по периодам
      </Typography>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <PeriodDynamicsChart
          series={dynamicsSeries}
          title={undefined}
          yAxisTitle={config.chartYAxisTitle}
          zeroLine={config.zeroLine}
          valueSuffix={config.unit}
          valueDigits={config.digits}
          testId="trend-detail-dynamics-chart"
        />
      </Paper>

      {mode !== 'plan' ? (
        <>
          <Typography variant="h6" component="h2">
            Разбор изменения по факторам
          </Typography>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <FactorWaterfallChart
              startLabel="Было"
              startValue={config.fact[0]}
              endLabel="Стало"
              steps={config.waterfallSteps}
              goodDirection={config.goodDirection}
              valueFormatter={(v) => formatSigned(v, config.digits, config.unit)}
              testId="trend-detail-waterfall"
            />
          </Paper>
        </>
      ) : (
        <Alert severity="info" data-testid="trend-detail-waterfall-plan-note">
          Разбор по факторам показывается для факта — план по конструкции близок к норме и почти не меняется
          в течение периода.
        </Alert>
      )}

      <Typography variant="h6" component="h2">
        Версии в выборке периода
      </Typography>
      <VersionsTable rows={versions} unit={config.unit} digits={config.digits} />
    </Stack>
  );
}

export default function TrendDetailMockup() {
  const [tab, setTab] = useState<MockTab>('z-score');
  const config = CONFIGS[tab];

  return (
    <Box sx={{ bgcolor: 'background.default', minHeight: '100vh', py: 3 }}>
      <Container maxWidth="lg">
        <Stack spacing={2}>
          <Alert severity="warning" data-testid="trend-detail-mockup-banner">
            Мокап предложенной детализации — статичные демо-данные, backend-агрегация по под-периодам и
            факторам ещё не спроектирована.
          </Alert>

          <Box>
            <Typography variant="h5" component="h1">
              {config.pageTitle}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {config.pageSubtitle}
            </Typography>
          </Box>

          <Tabs value={tab} onChange={(_, v: MockTab) => setTab(v)} data-testid="trend-detail-tabs">
            {(Object.keys(CONFIGS) as MockTab[]).map((key) => (
              <Tab key={key} value={key} label={CONFIGS[key].tabLabel} data-testid={`trend-detail-tab-${key}`} />
            ))}
          </Tabs>

          <MetricTabContent config={config} />

          <Divider sx={{ my: 1 }} />
          <Typography variant="subtitle2">Что предложено дополнительно</Typography>
          <Grid container spacing={2}>
            {[
              {
                title: 'Таблица версий в выборке',
                text: 'Список основных версий факта/плана, попавших в период, с их значением — заменяет всплывающую подсказку ⓘ на архивной вкладке постоянно видимой таблицей.',
              },
              {
                title: 'Разбор по факторам (водопад)',
                text: 'Показывает, какие направления IIC внесли наибольший вклад в изменение среднего показателя — отвечает на вопрос «за счёт чего выросло/упало».',
              },
              {
                title: 'Единый переключатель Факт/План',
                text: 'Заменяет пару чекбоксов одним сегментированным контролом с третьим состоянием «Факт и план» для сравнения на одном графике.',
              },
            ].map((item) => (
              <Grid item xs={12} md={4} key={item.title}>
                <Paper variant="outlined" sx={{ p: 2, height: '100%' }}>
                  <Typography variant="subtitle2" sx={{ mb: 0.5 }}>
                    {item.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {item.text}
                  </Typography>
                </Paper>
              </Grid>
            ))}
          </Grid>
        </Stack>
      </Container>
    </Box>
  );
}
