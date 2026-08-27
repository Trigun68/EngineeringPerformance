import { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Box, Stack, Tab, Tabs, Typography } from '@mui/material';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LightModeIcon from '@mui/icons-material/LightMode';
import RefreshIcon from '@mui/icons-material/Refresh';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import { ToolbarActionButton } from '../../../components/export/ToolbarActionButton';
import { Activity, Moon, Sun } from 'lucide-react';
import {
  exportPerformanceReportPdf,
  fetchPerformanceReport,
  type PerformanceReportDto,
} from '../../../api/performanceReportApi';
import type { Project } from '../../../types/project';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { PanelBusyBar } from '../../../components/WorkingOverlay/PanelBusyBar';
import { usePermission } from '../../../hooks/usePermission';
import { usePersistedTabState } from '../../../hooks/usePersistedTabState';
import { getApiErrorMessage } from '../../../services/apiError';
import { reportDarkPrefKey } from '../../../utils/projectWorkspacePrefs';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { ReportsTab } from '../../../pages/projects/reports/ReportsTab';
import { DIRECTION_FACT, DIRECTION_PLAN } from '../../../pages/projects/reports/reportCalc';
import { DirectionsTable } from './DirectionsTable';
import { InsightsPanel } from './InsightsPanel';
import './report-theme.css';
import { SummaryCards } from './SummaryCards';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';
import { VersionTrend } from './VersionTrend';
import { ZScale } from './ZScale';

type ReportSubTab = 'summary' | 'legacy';

/**
 * Версия для детальных секций «Сводного» — зеркало backend
 * PerformanceReportService.ResolveAssessmentVersionId: primary Fact → primary Plan →
 * последняя Fact → последняя Plan (по CreatedAt).
 */
function resolveSummaryVersionId(versions: ProjectVersion[]): number | null {
  const pool = versions.filter((v) => !v.isArchived);
  if (pool.length === 0) return null;
  const facts = pool.filter((v) => v.direction === DIRECTION_FACT);
  const plans = pool.filter((v) => v.direction === DIRECTION_PLAN);
  const byCreatedDesc = (a: ProjectVersion, b: ProjectVersion) =>
    new Date(b.createdAt ?? 0).getTime() - new Date(a.createdAt ?? 0).getTime() || b.id - a.id;
  return (
    facts.find((v) => v.isPrimary)?.id ??
    plans.find((v) => v.isPrimary)?.id ??
    [...facts].sort(byCreatedDesc)[0]?.id ??
    [...plans].sort(byCreatedDesc)[0]?.id ??
    null
  );
}

export interface ReportPanelProps {
  projectId: number;
  project?: Project;
  versions?: ProjectVersion[];
}

export function ReportPanel({ projectId, project, versions }: ReportPanelProps) {
  const { canRead } = usePermission('Reports');
  const [report, setReport] = useState<PerformanceReportDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [darkRaw, setDarkRaw] = usePersistedTabState<'0' | '1'>(reportDarkPrefKey(projectId), '0');
  const dark = darkRaw === '1';
  const [subTab, setSubTab] = useState<ReportSubTab>('summary');

  const summaryVersionId = useMemo(
    () => (versions && versions.length > 0 ? resolveSummaryVersionId(versions) : null),
    [versions],
  );

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

  // Remount ReportsTab по nonce — перезапускает react-query загрузку новых вкладок
  // без необходимости тянуть QueryClient в панель (тесты рендерят панель без провайдера).
  const [refreshNonce, setRefreshNonce] = useState(0);
  const handleRefresh = useCallback(() => {
    void load();
    setRefreshNonce((n) => n + 1);
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
        <ToolbarActionButton
          startIcon={<RefreshIcon />}
          onClick={handleRefresh}
          disabled={loading}
        >
          {ACTION_LABELS.refresh}
        </ToolbarActionButton>
        {subTab === 'legacy' ? (
          <>
            <ExportActionButton
              kind="pdf"
              loading={exporting}
              onClick={() => void handlePdf()}
              disabled={exporting || !report}
              data-testid="performance-report-pdf"
            />
            <ToolbarActionButton
              startIcon={dark ? <LightModeIcon /> : <DarkModeIcon />}
              onClick={() => setDarkRaw(dark ? '0' : '1')}
              data-testid="performance-report-theme"
            >
              {dark ? 'Светлая' : 'Тёмная'}
            </ToolbarActionButton>
          </>
        ) : null}
      </Stack>

      {error ? (
        <Alert severity="error" sx={{ mb: 1.5 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      ) : null}

      {project && versions && versions.length > 0 ? (
        <Tabs
          value={subTab}
          onChange={(_, v: ReportSubTab) => setSubTab(v)}
          sx={{
            minHeight: 36,
            borderBottom: 1,
            borderColor: 'divider',
            mb: 1.5,
            '& .MuiTab-root': { minHeight: 36, textTransform: 'none' },
          }}
        >
          <Tab value="summary" label="Сводный вер 1" data-testid="report-summary-tab" />
          <Tab value="legacy" label="Сводный вер 2" data-testid="report-legacy-tab" />
        </Tabs>
      ) : null}

      {subTab !== 'legacy' && project && versions && versions.length > 0 ? (
        <Box sx={{ overflow: 'auto', maxHeight: 'calc(100vh - 260px)' }}>
          <ReportsTab
            key={refreshNonce}
            projectId={projectId}
            project={project}
            versions={versions}
            versionId={summaryVersionId ?? versions[0].id}
          />
        </Box>
      ) : (
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
                <DirectionsTable projectId={projectId} rows={report.directions} />
              </ReportCardContent>
            </ReportCard>
          </div>
        ) : null}
      </Box>
      )}
    </>
  );
}
