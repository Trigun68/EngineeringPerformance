import { useCallback, useEffect, useMemo, useState } from 'react';
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
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import {
  buildVersionLabel,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { WorkingOverlay } from '../../../components/WorkingOverlay/WorkingOverlay';
import { usePermission } from '../../../hooks/usePermission';
import { getApiErrorMessage } from '../../../services/apiError';
import { WorkspaceChromeToggleRow } from '../shared/WorkspaceChromeToggleRow';
import { AccuracyPanel } from './AccuracyPanel';
import { DirectionBars } from './DirectionBars';
import { DirectionsTable } from './DirectionsTable';
import { InsightsPanel } from './InsightsPanel';
import './report-theme.css';
import { SummaryCards } from './SummaryCards';
import { ReportBadge, ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';
import { VersionTrend } from './VersionTrend';
import { ZScale } from './ZScale';

const ANALYSIS_PLAN = 1;
const ANALYSIS_FACT = 2;

export interface ReportPanelProps {
  projectId: number;
  workspaceVersions: ProjectVersion[];
  workspacePlanVersionId: number | null;
  workspaceFactVersionId: number | null;
}

export function ReportPanel({
  projectId,
  workspaceVersions,
  workspacePlanVersionId,
  workspaceFactVersionId,
}: ReportPanelProps) {
  const { canRead } = usePermission('Reports');
  const [planVersionId, setPlanVersionId] = useState<number | null>(workspacePlanVersionId);
  const [factVersionId, setFactVersionId] = useState<number | null>(workspaceFactVersionId);
  const [planPickerOpen, setPlanPickerOpen] = useState(false);
  const [factPickerOpen, setFactPickerOpen] = useState(false);
  const [report, setReport] = useState<PerformanceReportDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dark, setDark] = useState(false);

  useEffect(() => {
    setPlanVersionId(workspacePlanVersionId);
  }, [workspacePlanVersionId]);

  useEffect(() => {
    setFactVersionId(workspaceFactVersionId);
  }, [workspaceFactVersionId]);

  const planLabel = useMemo(() => {
    const v = workspaceVersions.find((x) => x.id === planVersionId);
    return v ? buildVersionLabel(v) : 'Выберите версию плана';
  }, [planVersionId, workspaceVersions]);

  const factLabel = useMemo(() => {
    const v = workspaceVersions.find((x) => x.id === factVersionId);
    return v ? buildVersionLabel(v) : 'Выберите версию факта';
  }, [factVersionId, workspaceVersions]);

  const load = useCallback(async () => {
    if (!canRead || !projectId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchPerformanceReport(projectId, planVersionId, factVersionId);
      setReport(data);
    } catch (e) {
      setReport(null);
      setError(getApiErrorMessage(e, 'Не удалось загрузить отчёт производительности.'));
    } finally {
      setLoading(false);
    }
  }, [canRead, projectId, planVersionId, factVersionId]);

  useEffect(() => {
    void load();
  }, [load]);

  const handlePdf = async () => {
    setExporting(true);
    setError(null);
    try {
      await exportPerformanceReportPdf(projectId, planVersionId, factVersionId);
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

  const directions = report?.fact?.directions ?? report?.plan?.directions ?? [];

  return (
    <>
      <WorkspaceChromeToggleRow />
      <WorkingOverlay open={loading || exporting} />

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mb: 1.5 }}>
        <Button size="small" variant="outlined" onClick={() => setPlanPickerOpen(true)}>
          План: {planLabel}
        </Button>
        <Button size="small" variant="outlined" onClick={() => setFactPickerOpen(true)}>
          Факт: {factLabel}
        </Button>
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
                  <h1 className="text-xl font-semibold md:text-2xl">{report.project.name}</h1>
                </div>
                <div className="ml-auto flex items-center gap-2 text-muted-foreground">
                  {dark ? <Moon className="size-4" /> : <Sun className="size-4" />}
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                <span className="font-mono text-xs">{report.project.code}</span>
                {report.project.gip ? <span>ГИП: {report.project.gip}</span> : null}
                {report.project.customer ? <span>{report.project.customer}</span> : null}
                {report.plan ? (
                  <ReportBadge>План · {report.plan.versionName}</ReportBadge>
                ) : null}
                {report.fact ? (
                  <ReportBadge>Факт · {report.fact.versionName}</ReportBadge>
                ) : null}
                {report.project.workStatus ? (
                  <ReportBadge className="bg-primary text-primary-foreground border-transparent">
                    {report.project.workStatus}
                  </ReportBadge>
                ) : null}
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
            />

            <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
              <DirectionBars
                rows={report.plan?.directions ?? []}
                title="Направления — план"
                description="Корневые направления выбранной версии плана"
              />
              <DirectionBars
                rows={report.fact?.directions ?? []}
                title="Направления — факт"
                description="Корневые направления выбранной версии факта"
              />
            </div>

            <VersionTrend points={report.versionHistory} />

            <InsightsPanel
              insights={report.insights}
              totalHours={
                report.fact?.totalLaborHours ?? report.plan?.totalLaborHours ?? null
              }
            />

            <AccuracyPanel accuracy={report.accuracy} />

            <ReportCard>
              <ReportCardHeader>
                <ReportCardTitle>
                  Дерево производительности ({report.fact ? 'факт' : 'план'})
                </ReportCardTitle>
              </ReportCardHeader>
              <ReportCardContent>
                <DirectionsTable rows={directions} />
              </ReportCardContent>
            </ReportCard>
          </div>
        ) : null}
      </Box>

      <ProjectVersionPickerDialog
        open={planPickerOpen}
        title="Версия плана для отчёта"
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter={ANALYSIS_PLAN}
        selectedVersionId={planVersionId}
        onSelect={(v) => {
          setPlanVersionId(v.id);
          setPlanPickerOpen(false);
        }}
        onClose={() => setPlanPickerOpen(false)}
      />
      <ProjectVersionPickerDialog
        open={factPickerOpen}
        title="Версия факта для отчёта"
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter={ANALYSIS_FACT}
        selectedVersionId={factVersionId}
        onSelect={(v) => {
          setFactVersionId(v.id);
          setFactPickerOpen(false);
        }}
        onClose={() => setFactPickerOpen(false)}
      />
    </>
  );
}
