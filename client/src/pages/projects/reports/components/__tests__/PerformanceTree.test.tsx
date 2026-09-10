import { createTheme, ThemeProvider } from '@mui/material';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { PerformanceTreeRow } from '../../reportCalc';
import { PerformanceTree } from '../PerformanceTree';

const theme = createTheme();

function row(partial: Partial<PerformanceTreeRow> & Pick<PerformanceTreeRow, 'workDirectionId' | 'workDirectionName'>): PerformanceTreeRow {
  return {
    workDirectionCode: 'IIC',
    parentId: null,
    level: 0,
    normUnitName: 'ед.',
    factPhysicalVolume: null,
    factLaborHours: 12,
    factProductivity: 1.2,
    factEstimate: null,
    normProductivity: 1,
    normEstimate: null,
    normStdDev: 0.2,
    zScore: 0.4,
    interpretation: null,
    subtreeHours: 12,
    subtreePhysicalVolume: 18,
    physicalVolume: 2,
    assessmentPhysicalVolume: 18,
    children: [],
    ...partial,
  };
}

describe('PerformanceTree', () => {
  it('shows own and grouped physical volume and a single fact-hours column', () => {
    const leaf = row({
      workDirectionId: 11,
      workDirectionName: 'Лист',
      parentId: 1,
      level: 1,
      physicalVolume: 10,
      assessmentPhysicalVolume: null,
      factLaborHours: null,
      zScore: null,
    });
    const root = row({
      workDirectionId: 1,
      workDirectionName: 'Корень',
      children: [leaf],
    });

    render(
      <ThemeProvider theme={theme}>
        <PerformanceTree rows={[root]} />
      </ThemeProvider>,
    );

    expect(screen.getByText('Физобъём')).toBeInTheDocument();
    expect(screen.getByText('Сгруппированный физобъём')).toBeInTheDocument();
    expect(screen.getByText('Фактически часы')).toBeInTheDocument();
    expect(screen.queryByText('Факт. объём')).not.toBeInTheDocument();
    expect(screen.queryByText('Часы поддерева')).not.toBeInTheDocument();

    const bodyRows = screen.getAllByRole('row').slice(1);
    const rootCells = within(bodyRows[0]).getAllByRole('cell');
    expect(rootCells[1]).toHaveTextContent('2,0');
    expect(rootCells[2]).toHaveTextContent('18,0');
    expect(rootCells[3]).toHaveTextContent('12,0');

    const leafCells = within(bodyRows[1]).getAllByRole('cell');
    expect(leafCells[1]).toHaveTextContent('10,0');
    expect(leafCells[2]).toHaveTextContent('—');
    expect(leafCells[2]).not.toHaveTextContent('Н/Д');
    expect(leafCells[8]).toHaveTextContent('—');
  });

  it('collapses children like the previous tree', () => {
    const leaf = row({
      workDirectionId: 11,
      workDirectionName: 'Лист',
      parentId: 1,
      level: 1,
    });
    const root = row({
      workDirectionId: 1,
      workDirectionName: 'Корень',
      children: [leaf],
    });

    render(
      <ThemeProvider theme={theme}>
        <PerformanceTree rows={[root]} />
      </ThemeProvider>,
    );

    expect(screen.getByText('Лист')).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText('Свернуть Корень'));
    expect(screen.queryByText('Лист')).not.toBeInTheDocument();
  });
});
