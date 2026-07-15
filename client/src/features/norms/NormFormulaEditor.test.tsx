import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { NormFormulaEditor } from './NormFormulaEditor';
import { defaultNormFormula } from '../../types/norms';

vi.mock('highcharts/highcharts-more', () => ({ default: vi.fn() }));
vi.mock('highcharts', () => ({ default: {} }));
vi.mock('highcharts-react-official', () => ({
  default: () => <div data-testid="chart" />,
}));
vi.mock('../../api/normsApi', () => ({
  fetchFormulaTemplates: vi.fn().mockResolvedValue([
    { type: 1, displayName: 'Линейная', formulaLatex: 'a-bV', requiresBounds: false, coefficients: [{ name: 'a', label: 'a', required: true }] },
  ]),
  previewNormCurve: vi.fn().mockResolvedValue({ curve: [], band: [] }),
}));

function wrap(ui: React.ReactNode) {
  const client = new QueryClient();
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('NormFormulaEditor', () => {
  it('renders formula type selector', async () => {
    wrap(
      <NormFormulaEditor
        value={defaultNormFormula(100)}
        deviationFormula={{ modelType: 1, coefficients: { sigma: 0.1 } }}
        onChange={() => undefined}
      />,
    );
    expect(await screen.findByLabelText(/тип формулы/i)).toBeInTheDocument();
  });
});

describe('DeviationFormulaEditor', () => {
  it('renders model selector', async () => {
    const { DeviationFormulaEditor } = await import('./DeviationFormulaEditor');
    wrap(
      <DeviationFormulaEditor
        value={{ modelType: 1, coefficients: { sigma: 0.1 } }}
        onChange={() => undefined}
      />,
    );
    expect(screen.getByLabelText(/процент отклонения/i)).toBeInTheDocument();
  });
});
