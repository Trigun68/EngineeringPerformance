import { createTheme, ThemeProvider } from '@mui/material';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ForecastReportDialog } from '../ForecastReportDialog';

vi.mock('../ForecastReportView', () => ({
  ForecastReportView: () => <div>forecast-view</div>,
}));

const theme = createTheme();

describe('ForecastReportDialog', () => {
  it('puts project code, name and version into the dialog title', () => {
    render(
      <ThemeProvider theme={theme}>
        <ForecastReportDialog
          open
          projectId={1}
          versionId={2}
          projectCode="25-310"
          projectName="Реконструкция НС"
          versionLabel="Основная"
          summaryRows={[]}
          onClose={() => undefined}
        />
      </ThemeProvider>,
    );

    expect(
      screen.getByText('Прогноз трудозатрат: 25-310 — Реконструкция НС — Основная'),
    ).toBeInTheDocument();
    expect(screen.queryByText('25-310 — Реконструкция НС (Основная)')).not.toBeInTheDocument();
  });
});
