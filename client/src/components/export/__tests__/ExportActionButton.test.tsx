import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ACTION_LABELS } from '../../../constants/actionLabels';
import { ExportActionButton } from '../ExportActionButton';

describe('ExportActionButton', () => {
  it('renders canonical captions for each kind', () => {
    const { rerender } = render(<ExportActionButton kind="excel" />);
    expect(screen.getByRole('button', { name: ACTION_LABELS.exportExcel })).toBeInTheDocument();

    rerender(<ExportActionButton kind="pdf" />);
    expect(screen.getByRole('button', { name: ACTION_LABELS.exportPdf })).toBeInTheDocument();

    rerender(<ExportActionButton kind="excelSelected" />);
    expect(screen.getByRole('button', { name: ACTION_LABELS.exportExcelSelected })).toBeInTheDocument();

    rerender(<ExportActionButton kind="excelSummary" />);
    expect(screen.getByRole('button', { name: ACTION_LABELS.exportExcelSummary })).toBeInTheDocument();
  });

  it('keeps the same caption while loading', () => {
    render(<ExportActionButton kind="excel" loading />);
    expect(screen.getByRole('button', { name: ACTION_LABELS.exportExcel })).toBeDisabled();
  });
});
