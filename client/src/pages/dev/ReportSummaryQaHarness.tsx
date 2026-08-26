/**
 * ВРЕМЕННЫЙ харнес только для визуальной QA-проверки PlanFactSummary/ZScale/VersionTrend
 * с моковыми данными по трём сценариям из плана (раздел 5): (a) план+факт, (b) только план,
 * (c) несколько версий плана разных дат + одна факт-версия. Удалить после проверки — не
 * является частью продуктовой функциональности.
 */
import { Box, Stack, Typography } from '@mui/material';
import { PlanFactSummary } from '../projects/reports/components/PlanFactSummary';
import { ZScale } from '../projects/reports/components/ZScale';
import { VersionTrend } from '../projects/reports/components/VersionTrend';
import type { ProjectReportSummary } from '../../api/analyticsApi';
import type { VersionTrendPoint } from '../projects/reports/reportCalc';

const scenarioA: { summary: ProjectReportSummary; trend: VersionTrendPoint[]; accuracy: number | null } = {
  summary: {
    planVersionId: 1,
    planVersionName: 'Версия 2 (план)',
    planHours: 12400,
    planWeightedZScore: 0.12,
    planPercentile: '54-й',
    planInterpretation: null,
    factVersionId: 2,
    factVersionName: 'Версия 1 (факт)',
    factHours: 13650,
    factWeightedZScore: 0.68,
    factPercentile: '76-й',
    factInterpretation: null,
  },
  trend: [
    { versionId: 1, versionName: 'План v1', direction: 1, date: '01.02.2025', sortDate: 1, weightedZScore: 0.35, totalHours: 11800 },
    { versionId: 2, versionName: 'План v2', direction: 1, date: '15.03.2025', sortDate: 2, weightedZScore: 0.12, totalHours: 12400 },
    { versionId: 3, versionName: 'Факт v1', direction: 2, date: '10.04.2025', sortDate: 3, weightedZScore: 0.68, totalHours: 13650 },
  ],
  accuracy: 88.4,
};

const scenarioB: { summary: ProjectReportSummary; trend: VersionTrendPoint[]; accuracy: number | null } = {
  summary: {
    planVersionId: 1,
    planVersionName: 'Версия 1 (план)',
    planHours: 9800,
    planWeightedZScore: -0.42,
    planPercentile: '32-й',
    planInterpretation: null,
    factVersionId: null,
    factVersionName: null,
    factHours: null,
    factWeightedZScore: null,
    factPercentile: null,
    factInterpretation: null,
  },
  trend: [
    { versionId: 1, versionName: 'План v1', direction: 1, date: '05.01.2025', sortDate: 1, weightedZScore: -0.42, totalHours: 9800 },
  ],
  accuracy: 91.2,
};

const scenarioC: { summary: ProjectReportSummary; trend: VersionTrendPoint[]; accuracy: number | null } = {
  summary: {
    planVersionId: 3,
    planVersionName: 'Версия 3 (план)',
    planHours: 15200,
    planWeightedZScore: -0.85,
    planPercentile: '18-й',
    planInterpretation: null,
    factVersionId: 4,
    factVersionName: 'Версия 1 (факт)',
    factHours: 16100,
    factWeightedZScore: 1.34,
    factPercentile: '92-й',
    factInterpretation: null,
  },
  trend: [
    { versionId: 1, versionName: 'План v1', direction: 1, date: '01.11.2024', sortDate: 1, weightedZScore: 0.55, totalHours: 14000 },
    { versionId: 2, versionName: 'План v2', direction: 1, date: '20.12.2024', sortDate: 2, weightedZScore: 0.05, totalHours: 14600 },
    { versionId: 3, versionName: 'План v3', direction: 1, date: '18.02.2025', sortDate: 3, weightedZScore: -0.85, totalHours: 15200 },
    { versionId: 4, versionName: 'Факт v1', direction: 2, date: '30.03.2025', sortDate: 4, weightedZScore: 1.34, totalHours: 16100 },
  ],
  accuracy: 79.6,
};

function Scenario({
  title,
  data,
}: {
  title: string;
  data: { summary: ProjectReportSummary; trend: VersionTrendPoint[]; accuracy: number | null };
}) {
  return (
    <Stack spacing={2}>
      <Typography variant="h6">{title}</Typography>
      <PlanFactSummary summary={data.summary} accuracyScore={data.accuracy} />
      <ZScale
        planZScore={data.summary.planWeightedZScore}
        factZScore={data.summary.factWeightedZScore}
        percentile={data.summary.factPercentile}
      />
      <VersionTrend points={data.trend} />
    </Stack>
  );
}

export default function ReportSummaryQaHarness() {
  return (
    <Box sx={{ p: 3, bgcolor: '#F4F6F8', minHeight: '100vh' }}>
      <Stack spacing={5}>
        <Scenario title="(a) Основная версия плана + основная версия факта" data={scenarioA} />
        <Scenario title="(b) Только основная версия плана (факт не назначен)" data={scenarioB} />
        <Scenario title="(c) Несколько версий плана разных дат + одна версия факта" data={scenarioC} />
      </Stack>
    </Box>
  );
}
