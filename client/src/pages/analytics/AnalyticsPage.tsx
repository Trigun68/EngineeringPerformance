import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  Box,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { AnalyticsChromeToggleRow } from '../../features/analytics/AnalyticsChromeToggleRow';
import {
  AnalyticsChromeProvider,
} from '../../features/analytics/analyticsChromeContext';
import { AnalyticsPeriodFilters } from '../../features/analytics/AnalyticsPeriodFilters';
import {
  resolveAnalyticsPeriod,
  type AnalyticsPeriodPreset,
} from '../../features/analytics/analyticsPeriod';
import { FactNormAnalysisPanel } from '../../features/analytics/factNorm/FactNormAnalysisPanel';
import { NormAnalysisPanel } from '../../features/analytics/norm-analysis/NormAnalysisPanel';
import { ProductivityTrendPanel } from '../../features/analytics/productivity-trend/ProductivityTrendPanel';
import type { FactPlanMode } from '../../features/analytics/shared/FactPlanToggle';
import { TrendAnalysisPanel } from '../../features/analytics/trends/TrendAnalysisPanel';
import { projectsPageSubTabsSx } from '../../features/projects/shared/projectsPageTabStyles';
import { usePersistedTabState } from '../../hooks/usePersistedTabState';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import type { ProductivityVolumeMode } from '../../api/productivityTrendApi';
import type { ZTrendWeightMode } from '../../api/zTrendApi';

type AnalyticsMainTab = 'trends' | 'productivity-trend' | 'fact-norms' | 'norm-analysis';

const PERIOD_PREF_KEY = 'analytics.period.v1';
const CHROME_PREF_KEY = 'analytics.chrome.v1';
const TRENDS_PREF_KEY = 'analytics.trends.v1';
const PROD_TREND_PREF_KEY = 'analytics.productivityTrend.v1';

const MAIN_TABS: { value: AnalyticsMainTab; label: string; testId: string }[] = [
  { value: 'trends', label: 'Тренд оценки', testId: 'analytics-tab-trends' },
  {
    value: 'productivity-trend',
    label: 'Тренд оценки производительности',
    testId: 'analytics-tab-productivity-trend',
  },
  { value: 'fact-norms', label: 'Анализ факта и норм', testId: 'analytics-tab-fact-norms' },
  { value: 'norm-analysis', label: 'Анализ норм', testId: 'analytics-tab-norm-analysis' },
];

interface SavedPeriodPrefs {
  preset?: AnalyticsPeriodPreset;
  dateFrom?: string;
  dateTo?: string;
}

interface SavedTrendsPrefs {
  weightMode?: string;
  showPlan?: boolean;
  showFact?: boolean;
}

interface SavedProdTrendPrefs {
  volumeMode?: string;
}

const TAB_BLURB: Record<AnalyticsMainTab, string> = {
  trends:
    'Портфель: динамика Z по датам, мост часов (объём / производительность), Z групп IIC относительно нормы и накопительные часы факта. Разбор ΔZ и сегменты норм — в «Дополнительно».',
  'productivity-trend':
    'Соотношение факта и плана с нормативной выработкой по основным версиям проектов — на сколько процентов быстрее или медленнее нормы выполняется работа.',
  'fact-norms': 'Сверка фактической производительности с действующими нормами IIC: серьёзность отклонений и вердикт по группе.',
  'norm-analysis':
    'Покрытие направлений нормами, достоверность, дрейф-спарклайны, тепловая карта и симулятор «что если» без публикации нормы.',
};

export function AnalyticsPage() {
  const [mainTab, setMainTab] = usePersistedTabState<AnalyticsMainTab>('analytics.mainTab', 'trends');
  const [preset, setPreset] = useState<AnalyticsPeriodPreset>('1m');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [prefsLoaded, setPrefsLoaded] = useState(false);
  const [chromeExpanded, setChromeExpanded] = useState(true);
  const [weightMode, setWeightMode] = useState<ZTrendWeightMode>('basisHours');
  const [showPlan, setShowPlan] = useState(true);
  const [showFact, setShowFact] = useState(true);
  const [volumeMode, setVolumeMode] = useState<ProductivityVolumeMode>('median');
  const [factNormToolbarExtras, setFactNormToolbarExtras] = useState<ReactNode>(null);
  const [factNormDirectionIds, setFactNormDirectionIds] = useState<number[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        const chromeRaw = prefs[CHROME_PREF_KEY];
        if (chromeRaw === '0' || chromeRaw === 'false') setChromeExpanded(false);

        const trendsRaw = prefs[TRENDS_PREF_KEY];
        if (trendsRaw) {
          try {
            const saved = JSON.parse(trendsRaw) as SavedTrendsPrefs;
            if (saved.weightMode === 'basisHours' || saved.weightMode === 'uniform') {
              setWeightMode('basisHours');
            }
            if (typeof saved.showPlan === 'boolean') setShowPlan(saved.showPlan);
            if (typeof saved.showFact === 'boolean') setShowFact(saved.showFact);
          } catch {
            // defaults
          }
        }

        const prodRaw = prefs[PROD_TREND_PREF_KEY];
        if (prodRaw) {
          try {
            const saved = JSON.parse(prodRaw) as SavedProdTrendPrefs;
            if (saved.volumeMode === 'median' || saved.volumeMode === 'p10' || saved.volumeMode === 'p90') {
              setVolumeMode(saved.volumeMode);
            }
          } catch {
            // defaults
          }
        }

        const raw = prefs[PERIOD_PREF_KEY];
        if (raw) {
          try {
            const saved = JSON.parse(raw) as SavedPeriodPrefs;
            const nextPreset = saved.preset ?? '1m';
            setPreset(nextPreset);
            if (nextPreset === 'custom') {
              setDateFrom(saved.dateFrom ?? '');
              setDateTo(saved.dateTo ?? '');
            } else {
              const range = resolveAnalyticsPeriod(nextPreset);
              setDateFrom(range.dateFrom);
              setDateTo(range.dateTo);
            }
          } catch {
            const range = resolveAnalyticsPeriod('1m');
            setDateFrom(range.dateFrom);
            setDateTo(range.dateTo);
          }
        } else {
          const range = resolveAnalyticsPeriod('1m');
          setDateFrom(range.dateFrom);
          setDateTo(range.dateTo);
        }
        setPrefsLoaded(true);
      })
      .catch(() => {
        if (cancelled) return;
        const range = resolveAnalyticsPeriod('1m');
        setDateFrom(range.dateFrom);
        setDateTo(range.dateTo);
        setPrefsLoaded(true);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const handleChromeExpanded = useCallback((expanded: boolean) => {
    setChromeExpanded(expanded);
    void putUserPreferences({ [CHROME_PREF_KEY]: expanded ? '1' : '0' });
  }, []);

  const persistTrends = useCallback(
    (next: { weightMode: ZTrendWeightMode; showPlan: boolean; showFact: boolean }) => {
      void putUserPreferences({
        [TRENDS_PREF_KEY]: JSON.stringify(next satisfies SavedTrendsPrefs),
      });
    },
    [],
  );

  const handleWeightMode = (next: ZTrendWeightMode) => {
    setWeightMode(next);
    persistTrends({ weightMode: next, showPlan, showFact });
  };

  // Режим «Факт/План/Оба» — единый переключатель вкладки тренда; храним как пару флагов
  // для обратной совместимости с сохранёнными предпочтениями.
  const trendMode: FactPlanMode = showPlan && showFact ? 'both' : showFact ? 'fact' : 'plan';

  const handleTrendModeChange = (next: FactPlanMode) => {
    const nextShowPlan = next !== 'fact';
    const nextShowFact = next !== 'plan';
    setShowPlan(nextShowPlan);
    setShowFact(nextShowFact);
    persistTrends({ weightMode, showPlan: nextShowPlan, showFact: nextShowFact });
  };

  const handleVolumeMode = (next: ProductivityVolumeMode) => {
    setVolumeMode(next);
    void putUserPreferences({
      [PROD_TREND_PREF_KEY]: JSON.stringify({ volumeMode: next } satisfies SavedProdTrendPrefs),
    });
  };

  const persistPeriod = useCallback(
    (nextPreset: AnalyticsPeriodPreset, from: string, to: string) => {
      void putUserPreferences({
        [PERIOD_PREF_KEY]: JSON.stringify({
          preset: nextPreset,
          dateFrom: from,
          dateTo: to,
        } satisfies SavedPeriodPrefs),
      });
    },
    [],
  );

  const handlePresetChange = (next: AnalyticsPeriodPreset) => {
    setPreset(next);
    if (next === 'custom') {
      persistPeriod(next, dateFrom, dateTo);
      return;
    }
    const range = resolveAnalyticsPeriod(next);
    setDateFrom(range.dateFrom);
    setDateTo(range.dateTo);
    persistPeriod(next, range.dateFrom, range.dateTo);
  };

  const handleDateFromChange = (value: string) => {
    setDateFrom(value);
    persistPeriod('custom', value, dateTo);
  };

  const handleDateToChange = (value: string) => {
    setDateTo(value);
    persistPeriod('custom', dateFrom, value);
  };

  const trendsExtras = useMemo(
    () => (
      <FormControl size="small" sx={{ minWidth: 180 }}>
        <InputLabel id="ztrend-weight-label">Вес</InputLabel>
        <Select
          labelId="ztrend-weight-label"
          label="Вес"
          value={weightMode}
          onChange={(e) => handleWeightMode(e.target.value as ZTrendWeightMode)}
          data-testid="ztrend-weight-mode"
        >
          <MenuItem value="basisHours">Нормативные часы (basisHours)</MenuItem>
        </Select>
      </FormControl>
    ),
    // handleWeightMode пересоздаётся каждый рендер (зависит от showPlan/showFact) — включаем в deps.
    [weightMode, handleWeightMode],
  );

  const productivityExtras = useMemo(
    () => (
      <FormControl size="small" sx={{ minWidth: 220 }}>
        <InputLabel id="prod-trend-volume-label">Опорный объём V*</InputLabel>
        <Select
          labelId="prod-trend-volume-label"
          label="Опорный объём V*"
          value={volumeMode}
          onChange={(e) => handleVolumeMode(e.target.value as ProductivityVolumeMode)}
          data-testid="prod-trend-volume-mode"
        >
          <MenuItem value="median">Медиана факта</MenuItem>
          <MenuItem value="p10">P10 факта</MenuItem>
          <MenuItem value="p90">P90 факта</MenuItem>
        </Select>
      </FormControl>
    ),
    [volumeMode],
  );

  const toolbarExtras =
    mainTab === 'trends'
      ? trendsExtras
      : mainTab === 'productivity-trend'
        ? productivityExtras
        : mainTab === 'fact-norms'
          ? factNormToolbarExtras
          : null;

  return (
    <AnalyticsChromeProvider chromeExpanded={chromeExpanded} setChromeExpanded={handleChromeExpanded}>
      <Box
        sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}
        data-testid="analytics-page"
      >
        <Box>
          <Typography variant="h5" component="h1">
            Аналитика
          </Typography>
          <Typography variant="body2" color="text.secondary" data-testid="analytics-tab-blurb">
            {TAB_BLURB[mainTab]}
          </Typography>
        </Box>

        {chromeExpanded ? (
          <>
            <Tabs
              value={mainTab}
              onChange={(_, v: AnalyticsMainTab) => setMainTab(v)}
              sx={projectsPageSubTabsSx}
              variant="scrollable"
              allowScrollButtonsMobile
              data-testid="analytics-main-tabs"
            >
              {MAIN_TABS.map((tab) => (
                <Tab key={tab.value} label={tab.label} value={tab.value} data-testid={tab.testId} />
              ))}
            </Tabs>

            {prefsLoaded ? (
              <AnalyticsPeriodFilters
                preset={preset}
                dateFrom={dateFrom}
                dateTo={dateTo}
                onPresetChange={handlePresetChange}
                onDateFromChange={handleDateFromChange}
                onDateToChange={handleDateToChange}
                extras={toolbarExtras}
              />
            ) : null}
          </>
        ) : (
          <Tabs
            value={mainTab}
            onChange={(_, v: AnalyticsMainTab) => setMainTab(v)}
            sx={projectsPageSubTabsSx}
            variant="scrollable"
            allowScrollButtonsMobile
            data-testid="analytics-main-tabs"
          >
            {MAIN_TABS.map((tab) => (
              <Tab key={tab.value} label={tab.label} value={tab.value} data-testid={tab.testId} />
            ))}
          </Tabs>
        )}

        <AnalyticsChromeToggleRow />

        {mainTab === 'trends' && prefsLoaded ? (
          <TrendAnalysisPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            weightMode={weightMode}
            mode={trendMode}
            onModeChange={handleTrendModeChange}
          />
        ) : null}

        {mainTab === 'productivity-trend' && prefsLoaded ? (
          <ProductivityTrendPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            volumeMode={volumeMode}
          />
        ) : null}

        {mainTab === 'fact-norms' && prefsLoaded ? (
          <FactNormAnalysisPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            onToolbarExtras={setFactNormToolbarExtras}
            workDirectionIds={factNormDirectionIds}
          />
        ) : null}

        {mainTab === 'norm-analysis' && prefsLoaded ? (
          <NormAnalysisPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            onOpenFactNorms={(groupId) => {
              setFactNormDirectionIds([groupId]);
              setMainTab('fact-norms');
            }}
          />
        ) : null}
      </Box>
    </AnalyticsChromeProvider>
  );
}
