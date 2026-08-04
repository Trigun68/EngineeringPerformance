import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { HierarchicalSelect } from '../HierarchicalSelect';

const items = [
  { id: 1, name: 'Root', code: 'R1', parentId: null },
  { id: 2, name: 'Child', code: 'C1', parentId: 1 },
];

describe('HierarchicalSelect', () => {
  it('renders empty option and calls onChange', async () => {
    const onChange = vi.fn();
    render(<HierarchicalSelect items={items} value={null} onChange={onChange} />);

    const input = screen.getByRole('combobox');
    fireEvent.mouseDown(input);
    fireEvent.click(await screen.findByText('C1 — Child'));

    expect(onChange).toHaveBeenCalledWith(2);
  });

  it('filters options by search text', async () => {
    render(<HierarchicalSelect items={items} value={null} onChange={() => undefined} />);

    const input = screen.getByRole('combobox');
    fireEvent.change(input, { target: { value: 'child' } });

    await waitFor(() => {
      expect(screen.getByText('C1 — Child')).toBeInTheDocument();
    });
  });
});
