import { act, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { PerformanceReportMergedDirectionRow } from '../../../../api/performanceReportApi';
import { DirectionsTable } from '../DirectionsTable';

vi.mock('../../../../api/userPreferencesApi', () => ({
  getUserPreferences: vi.fn(async () => ({})),
  putUserPreferences: vi.fn(async () => undefined),
}));

function row(
  partial: Partial<PerformanceReportMergedDirectionRow> &
    Pick<PerformanceReportMergedDirectionRow, 'workDirectionId' | 'name'>,
): PerformanceReportMergedDirectionRow {
  return {
    parentId: null,
    level: 0,
    code: 'C',
    unitName: 'шт',
    physicalVolume: 2,
    planLaborHours: 10,
    factLaborHours: 8,
    planSubtreeHours: 10,
    factSubtreeHours: 8,
    planProductivity: 1,
    factProductivity: 0.8,
    normProductivity: 1,
    normStdDev: 0.1,
    planZScore: -0.5,
    factZScore: 0.2,
    planInterpretation: null,
    factInterpretation: null,
    subtreePhysicalVolume: 50,
    assessmentPhysicalVolume: 18,
    ...partial,
  };
}

describe('DirectionsTable', () => {
  it('shows own physical volume on every row and grouped volume after it', async () => {
    const leaf = row({
      workDirectionId: 11,
      name: 'Лист',
      parentId: 1,
      level: 1,
      physicalVolume: 10,
      assessmentPhysicalVolume: null,
      subtreePhysicalVolume: 10,
    });
    const root = row({
      workDirectionId: 1,
      name: 'Корень',
    });

    await act(async () => {
      render(<DirectionsTable projectId={1} rows={[root, leaf]} />);
    });

    expect(screen.getByText('Физобъём')).toBeInTheDocument();
    expect(screen.getByText('Сгруппированный физобъём')).toBeInTheDocument();
    expect(screen.queryByText('Объём')).not.toBeInTheDocument();

    const bodyRows = screen.getAllByRole('row').slice(1);
    const rootCells = within(bodyRows[0]).getAllByRole('cell');
    expect(rootCells[1]).toHaveTextContent('2,0');
    expect(rootCells[1]).not.toHaveTextContent('50');
    expect(rootCells[2]).toHaveTextContent('18,0');

    const leafCells = within(bodyRows[1]).getAllByRole('cell');
    expect(leafCells[1]).toHaveTextContent('10,0');
    expect(leafCells[2]).toHaveTextContent('—');
  });
});
