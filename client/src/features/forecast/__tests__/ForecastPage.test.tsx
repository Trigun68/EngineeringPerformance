import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ForecastPage } from '../ForecastPage';

const fetchProjectVersions = vi.fn();
const fetchForecast = vi.fn();
const fetchPerformance = vi.fn();
const calculateForecast = vi.fn();
const importProjectData = vi.fn();

vi.mock('../../../hooks/usePermission', () => ({
  usePermission: (name: string) => ({
    canRead: true,
    canEdit: name !== 'Planning',
    hasLevel: () => true,
    level: 2,
    isAdmin: true,
  }),
}));

vi.mock('../../../api/projectVersionsApi', () => ({
  fetchProjectVersions: (...args: unknown[]) => fetchProjectVersions(...args),
  createProjectVersion: vi.fn(),
  updateProjectVersion: vi.fn(),
  copyProjectVersion: vi.fn(),
  deleteProjectVersion: vi.fn(),
}));

vi.mock('../../../api/analyticsApi', () => ({
  fetchForecast: (...args: unknown[]) => fetchForecast(...args),
  fetchPerformance: (...args: unknown[]) => fetchPerformance(...args),
  calculateForecast: (...args: unknown[]) => calculateForecast(...args),
  previewForecast: vi.fn(),
  saveForecastPlannedVolumes: vi.fn(),
}));

vi.mock('../../../api/importApi', () => ({
  importProjectData: (...args: unknown[]) => importProjectData(...args),
}));

vi.mock('../../../components/dictionary/DictionaryGrid', () => ({
  DictionaryGrid: ({ gridId }: { gridId: string }) => (
    <div data-testid="dictionary-grid" data-grid-id={gridId} />
  ),
}));

const baseReport = {
  projectName: 'P1',
  projectCode: 'P1',
  versionName: 'v1',
  projectCharacteristics: {},
  plannedVolumes: [
    {
      workDirectionId: 1,
      parentId: null,
      name: 'Root',
      unitName: 'м³',
      normProductivity: 2,
      normEstimate: null,
      normStandardDeviation: 1,
      plannedVolume: 10,
      optimisticHours: 8,
      baseHours: 20,
      pessimisticHours: 25,
      range: '8 - 25',
      hierarchyPath: 'Root',
    },
  ],
  scenarios: [
    { scenarioName: 'Оптимистичный', zScore: -0.5, totalLaborHours: 8, disciplines: [] },
    { scenarioName: 'Базовый', zScore: 0, totalLaborHours: 20, disciplines: [] },
    { scenarioName: 'Пессимистичный', zScore: 0.5, totalLaborHours: 25, disciplines: [] },
  ],
  top3Disciplines: [],
  interpretations: [],
};

describe('ForecastPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchProjectVersions.mockResolvedValue([
      { id: 10, projectId: 1, name: 'v1', versionName: 'v1', direction: 1, sortOrder: 0, isArchived: false },
    ]);
    fetchForecast.mockResolvedValue(baseReport);
    fetchPerformance.mockResolvedValue([]);
    calculateForecast.mockResolvedValue({
      ...baseReport,
      scenarios: [
        { scenarioName: 'Оптимистичный', zScore: -0.3, totalLaborHours: 9, disciplines: [] },
        { scenarioName: 'Базовый', zScore: 0, totalLaborHours: 21, disciplines: [] },
        { scenarioName: 'Пессимистичный', zScore: 0.7, totalLaborHours: 26, disciplines: [] },
      ],
    });
  });

  it('renders three data tabs and editable Z-score table', async () => {
    render(<ForecastPage projectId={1} />);

    expect(await screen.findByRole('tab', { name: 'План' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Факт' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Прогноз' })).toBeInTheDocument();
    expect(screen.getByText('Пессимистичный')).toBeInTheDocument();
    expect(screen.getByText('Базовый')).toBeInTheDocument();
    expect(screen.getByText('Оптимистичный')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Рассчитать' })).toBeInTheDocument();
  });

  it('switches tabs without losing cached Z-score values', async () => {
    render(<ForecastPage projectId={1} />);

    await screen.findByRole('tab', { name: 'План' });
    const zInputs = screen.getAllByRole('spinbutton');
    expect(zInputs.length).toBeGreaterThanOrEqual(3);
    fireEvent.change(zInputs[0], { target: { value: '0.9' } });

    fireEvent.click(screen.getByRole('tab', { name: 'План' }));
    fireEvent.click(screen.getByRole('tab', { name: 'Прогноз' }));

    expect(screen.getAllByRole('spinbutton')[0]).toHaveValue(0.9);
  });

  it('loads forecast grid after calculate', async () => {
    render(<ForecastPage projectId={1} />);

    await screen.findByRole('button', { name: 'Рассчитать' });
    fireEvent.click(screen.getByRole('button', { name: 'Рассчитать' }));

    await waitFor(() => expect(calculateForecast).toHaveBeenCalled());
    expect(await screen.findByTestId('dictionary-grid')).toHaveAttribute(
      'data-grid-id',
      'forecast-1-forecast',
    );
  });
});
