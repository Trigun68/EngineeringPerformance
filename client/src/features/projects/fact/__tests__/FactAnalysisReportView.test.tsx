import { render, screen, waitFor, within } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { FactAnalysisReportView } from '../FactAnalysisReportView';

function renderView(ui: ReactElement) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

vi.mock('../../../../hooks/usePermission', () => ({
  usePermission: () => ({ canRead: true }),
}));

vi.mock('../../../../api/analyticsApi', () => ({
  fetchVersionAnalysis: vi.fn(async () => ({
    projectName: 'Проект',
    projectCode: 'P-1',
    versionName: 'v1',
    directions: [
      {
        id: 10,
        parentId: null,
        level: 0,
        code: 'P',
        name: 'Родитель',
        unitName: 'м³',
        factPhysicalVolume: null,
        physicalVolume: null,
        assessmentPhysicalVolume: null,
        factLaborHours: null,
        pessimisticHours: 0,
        baseHours: 0,
        optimisticHours: 0,
        factProductivity: null,
        factEstimate: null,
        normProductivity: 1.2,
        normEstimate: null,
        normStdDev: null,
        zScore: 0.5,
        interpretation: 'Средняя производительность',
        physicalVolumes: [],
        equipmentVolumes: [],
        directionLaborIntensities: [],
      },
      {
        id: 11,
        parentId: 10,
        level: 1,
        code: 'L',
        name: 'Лист',
        unitName: 'м³',
        factPhysicalVolume: 8,
        physicalVolume: 8,
        assessmentPhysicalVolume: 15,
        factLaborHours: 12,
        pessimisticHours: 60,
        baseHours: 50,
        optimisticHours: 40,
        factProductivity: 0.5,
        factEstimate: null,
        normProductivity: 1.2,
        normEstimate: null,
        normStdDev: 0.1,
        zScore: 1.2,
        interpretation: 'Хорошая производительность',
        physicalVolumes: [
          { code: 'K1', name: 'Позиция', unitName: 'м³', quantity: 4, coefficient: 2, total: 8 },
        ],
        equipmentVolumes: [],
        directionLaborIntensities: [
          { documentationCode: 'DOC-1', plannedHours: 50, actualHours: 12, brand: 'АР' },
        ],
      },
    ],
    laborIntensities: [
      {
        documentationCode: 'DOC-1',
        brand: 'АР',
        workDirectionName: 'Лист',
        plannedHours: 50,
        actualHours: 12,
      },
    ],
    summary: {
      totalFactLaborHours: 12,
      weightedZScore: 1.2,
      weightedZScorePercentile: 'P80',
      weightedZScoreInterpretation: 'Хорошая производительность',
      unallocatedLaborHours: 0,
      physicalVolumeTotalsByUnit: [{ unitName: 'м³', totalVolume: 8 }],
    },
  })),
  exportAnalysisExcel: vi.fn(),
  exportAnalysisPdf: vi.fn(),
}));

describe('FactAnalysisReportView', () => {
  it('shows hours as optimistic, base, pessimistic and a project total row', async () => {
    renderView(<FactAnalysisReportView projectId={1} versionId={2} />);

    await waitFor(() => {
      expect(screen.getByText('Шифр')).toBeInTheDocument();
    });
    const header = screen.getByText('Шифр').parentElement;
    expect(header?.textContent).toMatch(
      /ШифрНаправлениеФизобъёмСгруппированный физобъёмФакт чПлан\.Труд\.Оптим\.План\.Труд\.Баз\.План\.Труд\.Песс\./,
    );
    expect(screen.getByText('По проекту')).toBeInTheDocument();
  });

  it('keeps child cards under the parent and does not roll up hours', async () => {
    renderView(<FactAnalysisReportView projectId={1} versionId={2} />);

    const parentCard = await screen.findByTestId('fact-analysis-card-10');
    const childCard = await screen.findByTestId('fact-analysis-card-11');
    expect(parentCard.compareDocumentPosition(childCard) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    expect(parentCard).toHaveTextContent('0,0');
    expect(parentCard).not.toHaveTextContent('40,0');
    expect(parentCard).not.toHaveTextContent('50,0');
    expect(parentCard).not.toHaveTextContent('60,0');
    expect(parentCard).not.toHaveTextContent('12,0');

    expect(childCard).toHaveTextContent('40,0');
    expect(childCard).toHaveTextContent('50,0');
    expect(childCard).toHaveTextContent('60,0');
    expect(childCard).toHaveTextContent('12,0');
  });

  it('shows own physical volumes and labor only on the leaf card', async () => {
    renderView(<FactAnalysisReportView projectId={1} versionId={2} />);

    const parentCard = await screen.findByTestId('fact-analysis-card-10');
    expect(within(parentCard).queryByText('Позиция')).not.toBeInTheDocument();
    expect(within(parentCard).queryByText('K1')).not.toBeInTheDocument();
    expect(within(parentCard).queryByText('DOC-1')).not.toBeInTheDocument();

    const childCard = await screen.findByTestId('fact-analysis-card-11');
    expect(within(childCard).getByText('Позиция')).toBeInTheDocument();
    expect(within(childCard).getByText('K1')).toBeInTheDocument();
    expect(within(childCard).getByText('DOC-1')).toBeInTheDocument();
    expect(within(childCard).getByText('Факт, ч')).toBeInTheDocument();
    expect(within(childCard).getByText('План, ч')).toBeInTheDocument();
    expect(within(childCard).getAllByText('8,00').length).toBeGreaterThanOrEqual(2);
    expect(within(childCard).getByText('15,00')).toBeInTheDocument();
    expect(within(parentCard).queryByText('15,00')).not.toBeInTheDocument();
  });
});
