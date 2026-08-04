import { useCallback, useEffect, useState } from 'react';
import { Box, Tab, Tabs, Typography } from '@mui/material';
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

type AnalyticsMainTab = 'trends' | 'fact-norms' | 'norm-analysis';

const PERIOD_PREF_KEY = 'analytics.period.v1';

interface SavedPeriodPrefs {
  preset?: AnalyticsPeriodPreset;
  dateFrom?: string;
  dateTo?: string;
}

export function AnalyticsPage() {
  const [mainTab, setMainTab] = usePersistedTabState<AnalyticsMainTab>('analytics.mainTab', 'trends');
  const [preset, setPreset] = useState<AnalyticsPeriodPreset>('1m');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [prefsLoaded, setPrefsLoaded] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
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

  return (
    <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 2, minHeight: 0 }}>
      <Typography variant="h5" component="h1">
        Аналитика
      </Typography>

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

      {prefsLoaded && mainTab !== 'norm-analysis' && (
        <AnalyticsPeriodFilters
          preset={preset}
          dateFrom={dateFrom}
          dateTo={dateTo}
          onPresetChange={handlePresetChange}
          onDateFromChange={handleDateFromChange}
          onDateToChange={handleDateToChange}
        />
      )}

      {mainTab === 'trends' && prefsLoaded && (
        <TrendAnalysisPanel dateFrom={dateFrom} dateTo={dateTo} />
      )}

      {mainTab === 'fact-norms' && prefsLoaded && (
        <FactNormAnalysisPanel dateFrom={dateFrom} dateTo={dateTo} />
      )}

      {mainTab === 'norm-analysis' && <NormAnalysisPanel />}
    </Box>
  );
}
