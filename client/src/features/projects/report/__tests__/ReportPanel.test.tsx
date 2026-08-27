import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportPanel } from '../ReportPanel';
import type { PerformanceReportDto } from '../../../../api/performanceReportApi';

const fetchPerformanceReport = vi.fn();

vi.mock('../../../../api/performanceReportApi', () => ({
  fetchPerformanceReport: (...args: unknown[]) => fetchPerformanceReport(...args),
  exportPerformanceReportPdf: vi.fn(),
}));

vi.mock('../../../../hooks/usePermission', () => ({
  usePermission: () => ({ canRead: true, canEdit: false }),
}));

vi.mock('../../../../api/userPreferencesApi', () => ({
  getUserPreferences: vi.fn(async () => ({})),
  putUserPreferences: vi.fn(async () => undefined),
}));

vi.mock('../../shared/WorkspaceChromeToggleRow', () => ({
  WorkspaceChromeToggleRow: () => null,
}));

const report: PerformanceReportDto = {
  project: {
    id: 1,
    code: 'P-1',
    name: 'Проект',
    gip: null,
    customer: null,
    workStatus: null,
    type: null,
    planVersionCount: 1,
    factVersionCount: 1,
  },
  plan: {
    versionId: 2,
    versionName: '30.06.26',
    createdAt: '2026-06-30T00:00:00Z',
    direction: 1,
    weightedZScore: -0.4,
    weightedZScorePercentile: '60%',
    weightedZScoreInterpretation: 'Удовлетворительно',
    totalLaborHours: 1200,
    unallocatedLaborHours: null,
    physicalVolumeTotalsByUnit: [],
    directions: [],
  },
  fact: {
    versionId: 3,
    versionName: '30.06.26 (факт)',
    createdAt: '2026-06-30T00:00:00Z',
    direction: 2,
    weightedZScore: null,
    weightedZScorePercentile: '—',
    weightedZScoreInterpretation: null,
    totalLaborHours: null,
    unallocatedLaborHours: 51342,
    physicalVolumeTotalsByUnit: [],
    directions: [],
  },
  versionHistory: [],
  accuracy: { averagePercent: null, items: [] },
  insights: { items: [], topNegative: [], topPositive: [] },
  directions: [],
  normSourcesUsed: 1,
};

describe('ReportPanel', () => {
  beforeEach(() => {
    fetchPerformanceReport.mockReset();
    fetchPerformanceReport.mockResolvedValue(report);
  });

  it('shows plan and fact version names and does not fall back fact hours to plan', async () => {
    render(<ReportPanel projectId={1} />);
    await waitFor(() => {
      expect(screen.getByTestId('report-plan-version')).toHaveTextContent('План: 30.06.26');
    });
    expect(screen.getByTestId('report-fact-version')).toHaveTextContent('Факт: 30.06.26 (факт)');
    expect(screen.getByTestId('report-labor-fact')).toHaveTextContent('—');
    expect(screen.getByTestId('report-labor-plan')).not.toHaveTextContent('—');
  });
});
