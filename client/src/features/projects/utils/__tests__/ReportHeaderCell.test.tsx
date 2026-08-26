import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ReportHeaderCell } from '../ReportHeaderCell';
import { REPORT_COLUMN_HEADER_FONT_SIZE } from '../projectModalTitle';

describe('ReportHeaderCell', () => {
  it('keeps visible label text and inserts wrap hints after dots', () => {
    const { container } = render(<ReportHeaderCell>План.Труд.Оптим.</ReportHeaderCell>);
    expect(screen.getByText('План.Труд.Оптим.')).toBeInTheDocument();
    expect(container.querySelectorAll('wbr')).toHaveLength(2);
  });

  it('uses 1rem for report column headers', () => {
    expect(REPORT_COLUMN_HEADER_FONT_SIZE).toBe('1rem');
  });
});
