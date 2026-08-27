import { describe, expect, it, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import type { ColDef, GridApi, IRowNode } from 'ag-grid-community';
import { ExcelSetColumnFilter } from '../ExcelSetColumnFilter';

vi.mock('ag-grid-react', () => ({
  useGridFilter: () => undefined,
}));

function makeApi(rows: Record<string, unknown>[]): GridApi {
  const nodes = rows.map((data) => ({ data } as IRowNode));
  return {
    forEachNode: (cb: (node: IRowNode) => void) => {
      nodes.forEach(cb);
    },
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  } as unknown as GridApi;
}

describe('ExcelSetColumnFilter', () => {
  it('shows display labels but filters by stable filterValueGetter keys', () => {
    const colDef: ColDef = {
      colId: 'kfoId',
      headerName: 'Классификатор (КФО)',
      filterValueGetter: (p) => {
        const id = (p.data as { classifierId?: number | null })?.classifierId;
        return id == null ? '' : String(id);
      },
      valueGetter: (p) => {
        const id = (p.data as { classifierId?: number | null })?.classifierId;
        if (id === 5) return 'IIC-1-2 — Общие фундаменты (м3)';
        if (id === 11) return 'IIC-2-1 — Вспомогательные стальные конструкции (т)';
        return '';
      },
    };

    const rows = [
      { id: 1, classifierId: 5 },
      { id: 2, classifierId: 5 },
      { id: 3, classifierId: 11 },
      { id: 4, classifierId: null },
    ];

    const onModelChange = vi.fn();
    render(
      <ExcelSetColumnFilter
        model={null}
        onModelChange={onModelChange}
        colDef={colDef}
        api={makeApi(rows)}
        column={{} as never}
        getValue={() => null}
        onUiChange={() => undefined}
        rowModel={{} as never}
        doesRowPassOtherFilter={() => true}
        context={undefined}
      />,
    );

    expect(screen.getByText('IIC-1-2 — Общие фундаменты (м3)')).toBeInTheDocument();
    expect(screen.getByText('IIC-2-1 — Вспомогательные стальные конструкции (т)')).toBeInTheDocument();
    expect(screen.getByText('(Пусто)')).toBeInTheDocument();
    // Не показываем сырые id в чеклисте
    expect(screen.queryByText('5')).not.toBeInTheDocument();
    expect(screen.queryByText('11')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Снять все' }));
    fireEvent.click(screen.getByLabelText('IIC-1-2 — Общие фундаменты (м3)'));
    fireEvent.click(screen.getByRole('button', { name: 'Применить' }));

    expect(onModelChange).toHaveBeenCalled();
    const model = onModelChange.mock.calls.at(-1)?.[0] as { values: string[] };
    expect(model.values).toEqual(['5']);
  });
});