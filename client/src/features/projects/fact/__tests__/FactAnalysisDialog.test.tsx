import { createTheme, ThemeProvider } from '@mui/material';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { FactAnalysisDialog } from '../FactAnalysisDialog';

vi.mock('../FactAnalysisReportView', () => ({
  FactAnalysisReportView: () => <div>fact-view</div>,
}));

const theme = createTheme();

describe('FactAnalysisDialog', () => {
  it('puts project code, name and version into the dialog title', () => {
    render(
      <ThemeProvider theme={theme}>
        <FactAnalysisDialog
          open
          projectId={1}
          versionId={2}
          projectCode="25-310"
          projectName="Реконструкция НС"
          versionLabel="Основная Факт 2025"
          onClose={() => undefined}
        />
      </ThemeProvider>,
    );

    expect(
      screen.getByText('Анализ версии: 25-310 — Реконструкция НС — Основная Факт 2025'),
    ).toBeInTheDocument();
  });
});
