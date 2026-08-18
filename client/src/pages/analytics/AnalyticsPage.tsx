import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  Box,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Switch,
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
import { TrendAnalysisPanel } from '../../features/analytics/trends/TrendAnalysisPanel';
import { projectsPageSubTabsSx } from '../../features/projects/shared/projectsPageTabStyles';
import { usePersistedTabState } from '../../hooks/usePersistedTabState';
import { getUserPreferences, putUserPreferences } from '../../api/userPreferencesApi';
import type { ZTrendWeightMode } from '../../api/zTrendApi';

type AnalyticsMainTab = 'trends' | 'fact-norms' | 'norm-analysis';

const PERIOD_PREF_KEY = 'analytics.period.v1';
const CHROME_PREF_KEY = 'analytics.chrome.v1';

interface SavedPeriodPrefs {
  preset?: AnalyticsPeriodPreset;
  dateFrom?: string;
  dateTo?: string;
}

const TAB_BLURB: Record<AnalyticsMainTab, string> = {
  trends: 'Динамика средневзвешенного Z по основным версиям плана и факта.',
  'fact-norms': 'Сверка фактической производительности с действующими нормами IIC.',
  'norm-analysis': 'Реестр норм IIC, таймлайн действия и проверка здоровья.',
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
  const [trendToolbarExtras, setTrendToolbarExtras] = useState<ReactNode>(null);
  const [factNormToolbarExtras, setFactNormToolbarExtras] = useState<ReactNode>(null);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        const chromeRaw = prefs[CHROME_PREF_KEY];
        if (chromeRaw === '0' || chromeRaw === 'false') setChromeExpanded(false);

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
      <>
        <FormControl size="small" sx={{ minWidth: 180 }}>
          <InputLabel id="ztrend-weight-label">Вес</InputLabel>
          <Select
            labelId="ztrend-weight-label"
            label="Вес"
            value={weightMode}
            onChange={(e) => setWeightMode(e.target.value as ZTrendWeightMode)}
            data-testid="ztrend-weight-mode"
          >
            <MenuItem value="basisHours">Нормативные часы (basisHours)</MenuItem>
            <MenuItem value="uniform">Равный (uniform)</MenuItem>
          </Select>
        </FormControl>
        <FormControlLabel
          control={
            <Switch
              checked={showPlan}
              onChange={(_, v) => setShowPlan(v)}
              data-testid="ztrend-toggle-plan"
            />
          }
          label="План"
        />
        <FormControlLabel
          control={
            <Switch
              checked={showFact}
              onChange={(_, v) => setShowFact(v)}
              data-testid="ztrend-toggle-fact"
            />
          }
          label="Факт"
        />
        {trendToolbarExtras}
      </>
    ),
    [weightMode, showPlan, showFact, trendToolbarExtras],
  );

  const toolbarExtras =
    mainTab === 'trends' ? trendsExtras : mainTab === 'fact-norms' ? factNormToolbarExtras : null;

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
              data-testid="analytics-main-tabs"
            >
              <Tab label="Трендовый анализ" value="trends" data-testid="analytics-tab-trends" />
              <Tab
                label="Сравнение факта и норм"
                value="fact-norms"
                data-testid="analytics-tab-fact-norms"
              />
              <Tab
                label="Анализ норм"
                value="norm-analysis"
                data-testid="analytics-tab-norm-analysis"
              />
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
            data-testid="analytics-main-tabs"
          >
            <Tab label="Трендовый анализ" value="trends" data-testid="analytics-tab-trends" />
            <Tab
              label="Сравнение факта и норм"
              value="fact-norms"
              data-testid="analytics-tab-fact-norms"
            />
            <Tab
              label="Анализ норм"
              value="norm-analysis"
              data-testid="analytics-tab-norm-analysis"
            />
          </Tabs>
        )}

        <AnalyticsChromeToggleRow />

        {mainTab === 'trends' && prefsLoaded ? (
          <TrendAnalysisPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            weightMode={weightMode}
            showPlan={showPlan}
            showFact={showFact}
            onToolbarExtras={setTrendToolbarExtras}
          />
        ) : null}

        {mainTab === 'fact-norms' && prefsLoaded ? (
          <FactNormAnalysisPanel
            dateFrom={dateFrom}
            dateTo={dateTo}
            onToolbarExtras={setFactNormToolbarExtras}
          />
        ) : null}

        {mainTab === 'norm-analysis' && prefsLoaded ? (
          <NormAnalysisPanel dateFrom={dateFrom} dateTo={dateTo} />
        ) : null}
      </Box>
    </AnalyticsChromeProvider>
  );
}
