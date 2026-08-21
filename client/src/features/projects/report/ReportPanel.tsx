import { useCallback, useEffect, useState } from 'react';
import { Alert, Box, Button, Stack, Typography } from '@mui/material';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LightModeIcon from '@mui/icons-material/LightMode';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import RefreshIcon from '@mui/icons-material/Refresh';
import { Activity, Moon, Sun } from 'lucide-react';
import {
  exportPerformanceReportPdf,
  fetchPerformanceReport,
  type PerformanceReportDto,
} from '../../../api/performanceReportApi';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import { usePermission } from '../../../hooks/usePermission';
import { getApiErrorMessage } from '../../../services/apiError';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { DirectionsTable } from './DirectionsTable';
import { InsightsPanel } from './InsightsPanel';
import './report-theme.css';
import { SummaryCards } from './SummaryCards';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';
import { VersionTrend } from './VersionTrend';
import { ZScale } from './ZScale';

export interface ReportPanelProps {
  projectId: number;
}

export function ReportPanel({ projectId }: ReportPanelProps) {
  const { canRead } = usePermission('Reports');
  const [report, setReport] = useState<PerformanceReportDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dark, setDark] = useState(false);

  const load = useCallback(async () => {
    if (!canRead || !projectId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchPerformanceReport(projectId);
      setReport(data);
    } catch (e) {
      setReport(null);
      setError(getApiErrorMessage(e, 'Не удалось загрузить отчёт производительности.'));
    } finally {
      setLoading(false);
    }
  }, [canRead, projectId]);

  useEffect(() => {
    void load();
  }, [load]);

  const handlePdf = async () => {
    setExporting(true);
    setError(null);
    try {
      await exportPerformanceReportPdf(projectId);
    } catch (e) {
      setError(getApiErrorMessage(e, 'Не удалось выгрузить PDF отчёта.'));
    } finally {
      setExporting(false);
    }
  };

  if (!canRead) {
    return (
      <>
        <WorkspaceChromeToggleRow />
        <Alert severity="warning">Нет прав на чтение отчётов (Reports).</Alert>
      </>
    );
  }

  return (
    <>
      <WorkspaceChromeToggleRow />
      <WorkingOverlay open={exporting} />
      <PanelBusyBar open={loading} />

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mb: 1.5 }}>
        <Button
          size="small"
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={() => void load()}
          disabled={loading}
        >
          Обновить
        </Button>
        <Button
          size="small"
          variant="contained"
          startIcon={<PictureAsPdfIcon />}
          onClick={() => void handlePdf()}
          disabled={exporting || !report}
          data-testid="performance-report-pdf"
        >
          Экспорт PDF
        </Button>
        <Button
          size="small"
          variant="outlined"
          startIcon={dark ? <LightModeIcon /> : <DarkModeIcon />}
          onClick={() => setDark((v) => !v)}
          data-testid="performance-report-theme"
        >
          {dark ? 'Светлая' : 'Тёмная'}
        </Button>
      </Stack>

      {error ? (
        <Alert severity="error" sx={{ mb: 1.5 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      ) : null}

      <Box
        className={`ep-report-root${dark ? ' dark' : ''}`}
        data-testid="performance-report-panel"
        sx={{
          borderRadius: 1,
          border: '1px solid',
          borderColor: 'divider',
          overflow: 'auto',
          maxHeight: 'calc(100vh - 220px)',
        }}
      >
        {!report && !loading ? (
          <Box sx={{ p: 3 }}>
            <Typography color="text.secondary">Нет данных отчёта.</Typography>
          </Box>
        ) : report ? (
          <div className="mx-auto flex max-w-[1400px] flex-col gap-6 px-4 py-6 md:px-8">
            <header className="flex flex-col gap-4 border-b border-border pb-6">
              <div className="flex flex-wrap items-center gap-3">
                <span className="flex size-9 items-center justify-center rounded-md bg-primary text-primary-foreground">
                  <Activity className="size-5" aria-hidden />
                </span>
                <div className="flex flex-col">
                  <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    EngineeringPerformance · Отчёт производительности
                  </span>
                  <h1 className="text-2xl font-semibold md:text-3xl">{report.project.name}</h1>
                </div>
                <div className="ml-auto flex items-center gap-2 text-muted-foreground">
                  {dark ? <Moon className="size-4" /> : <Sun className="size-4" />}
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
                <span className="font-mono text-xs">{report.project.code}</span>
                {report.plan?.versionName ? (
                  <span data-testid="report-plan-version">План: {report.plan.versionName}</span>
                ) : null}
                {report.fact?.versionName ? (
                  <span data-testid="report-fact-version">Факт: {report.fact.versionName}</span>
                ) : null}
                {report.project.gip ? <span>ГИП: {report.project.gip}</span> : null}
                {report.project.customer ? <span>Заказчик: {report.project.customer}</span> : null}
                {report.project.type ? <span>Тип: {report.project.type}</span> : null}
                {report.project.workStatus ? <span>Статус: {report.project.workStatus}</span> : null}
              </div>
            </header>

            <SummaryCards
              plan={report.plan}
              fact={report.fact}
              accuracyPercent={report.accuracy.averagePercent}
            />

            <ZScale
              planZ={report.plan?.weightedZScore ?? null}
              factZ={report.fact?.weightedZScore ?? null}
              planPercentile={report.plan?.weightedZScorePercentile ?? '—'}
              factPercentile={report.fact?.weightedZScorePercentile ?? '—'}
              normSourcesUsed={report.normSourcesUsed}
            />

            <VersionTrend points={report.versionHistory} />

            <InsightsPanel
              insights={report.insights}
              totalHours={report.fact?.totalLaborHours ?? null}
            />

            <ReportCard>
              <ReportCardHeader>
                <ReportCardTitle className="text-base md:text-lg">Направление</ReportCardTitle>
              </ReportCardHeader>
              <ReportCardContent>
                <DirectionsTable rows={report.directions} />
              </ReportCardContent>
            </ReportCard>
          </div>
        ) : null}
      </Box>
    </>
  );
}
