import { createTheme, ThemeProvider } from '@mui/material';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { HelpMarkdown } from '../HelpMarkdown';
import { NormAnalysisHelpDialog } from '../NormAnalysisHelpDialog';

const theme = createTheme();

describe('HelpMarkdown', () => {
  it('renders headings, bold, lists and tables', () => {
    render(
      <HelpMarkdown
        source={[
          '# Заголовок',
          '',
          'Текст с **жирным**.',
          '',
          '- пункт',
          '',
          '| A | B |',
          '|---|---|',
          '| x | y |',
        ].join('\n')}
      />,
    );
    expect(screen.getByRole('heading', { name: 'Заголовок' })).toBeInTheDocument();
    expect(screen.getByText('жирным')).toBeInTheDocument();
    expect(screen.getByText('пункт')).toBeInTheDocument();
    expect(screen.getByText('x')).toBeInTheDocument();
  });
});

describe('NormAnalysisHelpDialog', () => {
  it('shows title and CV interpretation when open', () => {
    render(
      <ThemeProvider theme={theme}>
        <NormAnalysisHelpDialog open onClose={() => undefined} />
      </ThemeProvider>,
    );
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Справка: анализ норм' })).toBeInTheDocument();
    expect(screen.getByText(/CV = \|σ \/ μ\|/)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Достоверность' })).toBeInTheDocument();
  });
});
