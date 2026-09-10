import { createTheme, ThemeProvider } from '@mui/material';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { ForecastIicVolumeRow } from '../../../../../api/analyticsApi';
import { ForecastTable } from '../ForecastTable';

const theme = createTheme();

function volumeRow(partial: Partial<ForecastIicVolumeRow> & Pick<ForecastIicVolumeRow, 'workDirectionId' | 'name'>): ForecastIicVolumeRow {
  return {
    parentId: null,
    unitName: 'ед.',
    normProductivity: 1,
    normEstimate: null,
    normStandardDeviation: 0.2,
    plannedVolume: 18,
    optimisticHours: 10,
    baseHours: 20,
    pessimisticHours: 30,
    range: '10 - 30',
    hierarchyPath: partial.name,
    ownPlannedVolume: 2,
    groupedPhysicalVolume: 18,
    ...partial,
  };
}

describe('ForecastTable', () => {
  it('shows own and grouped volumes, hides the 0.5σ note, and totals hours from roots only', () => {
    const rows: ForecastIicVolumeRow[] = [
      volumeRow({ workDirectionId: 1, name: 'Корень', parentId: null, ownPlannedVolume: 2, groupedPhysicalVolume: 18 }),
      volumeRow({
        workDirectionId: 11,
        name: 'Лист',
        parentId: 1,
        plannedVolume: 10,
        ownPlannedVolume: 10,
        groupedPhysicalVolume: null,
        optimisticHours: 4,
        baseHours: 8,
        pessimisticHours: 12,
      }),
    ];

    render(
      <ThemeProvider theme={theme}>
        <ForecastTable rows={rows} />
      </ThemeProvider>,
    );

    expect(screen.queryByText(/0\.5σ/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Плановый объём/)).not.toBeInTheDocument();
    expect(screen.getByText('Физобъём')).toBeInTheDocument();
    expect(screen.getByText('Сгруппированный физобъём')).toBeInTheDocument();

    const table = screen.getByRole('table');
    const footer = within(table).getAllByRole('row').at(-1)!;
    expect(footer).toHaveTextContent('Итого по проекту');
    expect(footer).toHaveTextContent('10,0');
    expect(footer).toHaveTextContent('20,0');
    expect(footer).toHaveTextContent('30,0');
    expect(footer).not.toHaveTextContent('14,0');
    expect(footer).not.toHaveTextContent('28,0');
  });

  it('dashes zero own volume and collapses children', () => {
    const rows: ForecastIicVolumeRow[] = [
      volumeRow({ workDirectionId: 1, name: 'Корень', ownPlannedVolume: 0, groupedPhysicalVolume: 10 }),
      volumeRow({
        workDirectionId: 11,
        name: 'Лист',
        parentId: 1,
        ownPlannedVolume: 10,
        groupedPhysicalVolume: null,
        optimisticHours: 1,
        baseHours: 1,
        pessimisticHours: 1,
      }),
    ];

    render(
      <ThemeProvider theme={theme}>
        <ForecastTable rows={rows} />
      </ThemeProvider>,
    );

    const bodyRows = screen.getAllByRole('row').slice(1, -1);
    const rootCells = within(bodyRows[0]).getAllByRole('cell');
    expect(rootCells[1]).toHaveTextContent('—');
    expect(rootCells[2]).toHaveTextContent('10,0');

    expect(screen.getByText('Лист')).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('Свернуть Корень'));
    expect(screen.queryByText('Лист')).not.toBeInTheDocument();
  });
});
