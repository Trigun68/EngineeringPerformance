import { render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { fetchForecastAssessmentVolumes } from '../../../../api/analyticsApi';
import { ForecastReportView } from '../ForecastReportView';
import type { ProjectsPageForecastSummaryRow } from '../../types';

vi.mock('../../../../api/projectsPageApi', () => ({
  exportForecastSummaryExcel: vi.fn(),
  exportForecastSummaryPdf: vi.fn(),
}));

vi.mock('../../../../api/analyticsApi', () => ({
  fetchForecastAssessmentVolumes: vi.fn(async () => ({
    physicalVolumesByDirection: {},
    equipmentVolumesByDirection: {},
  })),
}));

vi.mock('../../../../store/useDictionaryStore', () => ({
  useDictionaryStore: (selector: (s: { getById: () => null; getUnitName: () => string; fetchAll: () => void }) => unknown) =>
    selector({
      getById: () => null,
      getUnitName: () => '',
      fetchAll: () => undefined,
    }),
}));

const mockedVolumes = vi.mocked(fetchForecastAssessmentVolumes);

const rows: ProjectsPageForecastSummaryRow[] = [
  {
    workDirectionId: 1,
    displayName: 'ИТОГО',
    normUnitName: '',
    factPhysicalVolume: 1,
    normProductivity: null,
    baseHours: 100,
    optimisticHours: 80,
    pessimisticHours: 120,
    isTotal: true,
    level: -1,
    code: '',
  },
  {
    workDirectionId: 2,
    displayName: 'Направление',
    normUnitName: 'ч',
    factPhysicalVolume: 4,
    normProductivity: 1.5,
    baseHours: 50,
    optimisticHours: 40,
    pessimisticHours: 60,
    isTotal: false,
    level: 0,
    code: 'A1',
  },
];

const parentChildRows: ProjectsPageForecastSummaryRow[] = [
  {
    workDirectionId: 1,
    displayName: 'ИТОГО',
    normUnitName: '',
    factPhysicalVolume: null,
    normProductivity: null,
    baseHours: 50,
    optimisticHours: 40,
    pessimisticHours: 60,
    isTotal: true,
    level: -1,
    code: '',
  },
  {
    workDirectionId: 10,
    displayName: 'Родитель',
    normUnitName: 'м3',
    factPhysicalVolume: null,
    normProductivity: 1.2,
    baseHours: 0,
    optimisticHours: 0,
    pessimisticHours: 0,
    isTotal: false,
    level: 0,
    parentId: null,
    code: 'P',
  },
  {
    workDirectionId: 11,
    displayName: 'Лист',
    normUnitName: 'м3',
    factPhysicalVolume: 8,
    normProductivity: 1.2,
    baseHours: 50,
    optimisticHours: 40,
    pessimisticHours: 60,
    isTotal: false,
    level: 1,
    parentId: 10,
    code: 'L',
  },
];

describe('ForecastReportView', () => {
  it('shows hours as optimistic, base, pessimistic', async () => {
    mockedVolumes.mockResolvedValue({
      physicalVolumesByDirection: {},
      equipmentVolumesByDirection: {},
    });
    render(
      <ForecastReportView
        projectId={1}
        versionId={2}
        summaryRows={rows}
      />,
    );

    await waitFor(() => {
      expect(screen.getByText('Шифр')).toBeInTheDocument();
    });
    const header = screen.getByText('Шифр').parentElement;
    expect(header?.textContent).toMatch(
      /ШифрНаправлениеФакт ФОНорм\. произв\.Оптимист\., чБазовый, чПессимист\., ч/,
    );
    expect(screen.queryByText(/P-1 — Проект \(v1\)/)).not.toBeInTheDocument();
  });

  it('keeps parent card hours at own zeros and does not roll up the leaf', async () => {
    mockedVolumes.mockResolvedValue({
      physicalVolumesByDirection: {},
      equipmentVolumesByDirection: {},
    });
    render(
      <ForecastReportView
        projectId={1}
        versionId={2}
        summaryRows={parentChildRows}
      />,
    );

    const parentCard = await screen.findByTestId('forecast-report-card-10');
    expect(parentCard).toHaveTextContent('0,0');
    expect(parentCard).not.toHaveTextContent('40,0');
    expect(parentCard).not.toHaveTextContent('50,0');
    expect(parentCard).not.toHaveTextContent('60,0');
    expect(parentCard).not.toHaveTextContent('8,00');

    const leafCard = screen.getByTestId('forecast-report-card-11');
    expect(leafCard).toHaveTextContent('40,0');
    expect(leafCard).toHaveTextContent('50,0');
    expect(leafCard).toHaveTextContent('60,0');
  });

  it('shows own assessment volumes only; leaf FO does not appear on the parent card', async () => {
    mockedVolumes.mockResolvedValue({
      physicalVolumesByDirection: {
        '11': [
          { code: 'K1', name: 'Позиция', unitName: 'м3', quantity: 4, coefficient: 2, total: 8 },
        ],
      },
      equipmentVolumesByDirection: {},
    });
    render(
      <ForecastReportView
        projectId={1}
        versionId={2}
        summaryRows={parentChildRows}
      />,
    );

    const parentCard = await screen.findByTestId('forecast-report-card-10');
    expect(within(parentCard).queryByText('Позиция')).not.toBeInTheDocument();
    expect(within(parentCard).queryByText('K1')).not.toBeInTheDocument();

    const leafCard = screen.getByTestId('forecast-report-card-11');
    expect(within(leafCard).getByText('Позиция')).toBeInTheDocument();
    expect(within(leafCard).getByText('K1')).toBeInTheDocument();
    expect(within(leafCard).getAllByText('8,00').length).toBeGreaterThanOrEqual(2);
  });

  it('shows the parent own FO table when volumes are assigned to the parent node', async () => {
    mockedVolumes.mockResolvedValue({
      physicalVolumesByDirection: {
        '10': [
          { code: 'P1', name: 'Своя позиция', unitName: 'м3', quantity: 2, coefficient: 1, total: 2 },
        ],
      },
      equipmentVolumesByDirection: {},
    });
    render(
      <ForecastReportView
        projectId={1}
        versionId={2}
        summaryRows={parentChildRows}
      />,
    );

    const parentCard = await screen.findByTestId('forecast-report-card-10');
    expect(within(parentCard).getByText('Своя позиция')).toBeInTheDocument();
    expect(within(parentCard).getByText('P1')).toBeInTheDocument();
    expect(within(parentCard).getAllByText('2,00').length).toBeGreaterThanOrEqual(2);
  });
});
