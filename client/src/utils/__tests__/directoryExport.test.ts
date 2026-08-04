import { describe, expect, it } from 'vitest';
import type { Column, GridApi, IRowNode } from 'ag-grid-community';
import { buildExportFileName, collectGridExportData } from '../directoryExport';

describe('buildExportFileName', () => {
  it('sanitizes invalid characters in tab name', () => {
    const name = buildExportFileName('KFO/BER');
    expect(name).toMatch(/^KFO_BER_\d{8}_\d{6}\.xlsx$/);
  });

  it('uses fallback when tab name is empty after sanitize', () => {
    const name = buildExportFileName('???');
    expect(name).toMatch(/^Справочник_\d{8}_\d{6}\.xlsx$/);
  });
});

function mockColumn(colId: string, headerName: string, field?: string): Column {
  return {
    getColId: () => colId,
    getColDef: () => ({ headerName, field: field ?? colId }),
  } as unknown as Column;
}

function mockLeaf(data: Record<string, unknown>): IRowNode {
  return {
    group: false,
    footer: false,
    data,
    key: null,
    level: 0,
  } as unknown as IRowNode;
}

function mockGroup(partial: {
  key: string | null;
  level: number;
  footer?: boolean;
  aggData?: Record<string, unknown>;
}): IRowNode {
  return {
    group: true,
    footer: partial.footer ?? false,
    data: undefined,
    key: partial.key,
    level: partial.level,
    aggData: partial.aggData ?? {},
  } as unknown as IRowNode;
}

function createMockApi(opts: {
  columns: Column[];
  nodes: IRowNode[];
  rowGroupCount?: number;
  getValue: (col: Column, node: IRowNode) => unknown;
}): GridApi {
  return {
    getAllDisplayedColumns: () => opts.columns,
    getRowGroupColumns: () => Array.from({ length: opts.rowGroupCount ?? 0 }, () => ({})),
    forEachNodeAfterFilterAndSort: (cb: (node: IRowNode) => void) => {
      opts.nodes.forEach(cb);
    },
    getValue: opts.getValue,
  } as unknown as GridApi;
}

describe('collectGridExportData', () => {
  const nameCol = mockColumn('facilityName', 'Объект', 'facilityName');
  const hoursCol = mockColumn('baseHours', 'Базовый', 'baseHours');

  it('leaf-only by default skips group nodes (directories)', () => {
    const leafA = mockLeaf({ facilityName: 'A', baseHours: 10 });
    const group = mockGroup({ key: 'X', level: 0, aggData: { baseHours: 10 } });
    const api = createMockApi({
      columns: [nameCol, hoursCol],
      rowGroupCount: 1,
      nodes: [group, leafA],
      getValue: (col, node) => {
        if (node.group) return col.getColId() === 'baseHours' ? node.aggData?.baseHours : node.key;
        return node.data?.[col.getColId()];
      },
    });

    const { headers, rows } = collectGridExportData(api);
    expect(headers).toEqual(['Объект', 'Базовый']);
    expect(rows).toEqual([['A', '10']]);
  });

  it('without row groups includeGroups still exports leaves only', () => {
    const leaf = mockLeaf({ facilityName: 'A', baseHours: 5 });
    const api = createMockApi({
      columns: [nameCol, hoursCol],
      rowGroupCount: 0,
      nodes: [leaf],
      getValue: (col, node) => node.data?.[col.getColId()],
    });

    const { rows } = collectGridExportData(api, { includeGroups: true });
    expect(rows).toEqual([['A', '5']]);
  });

  it('includeGroups exports group header, leaves, footer and grand total', () => {
    const groupHeader = mockGroup({ key: 'Объект-1', level: 0, aggData: { baseHours: 30 } });
    const leaf1 = mockLeaf({ facilityName: 'Строка 1', baseHours: 10 });
    const leaf2 = mockLeaf({ facilityName: 'Строка 2', baseHours: 20 });
    const groupFooter = mockGroup({
      key: 'Объект-1',
      level: 0,
      footer: true,
      aggData: { baseHours: 30 },
    });
    const grandTotal = mockGroup({
      key: null,
      level: -1,
      footer: true,
      aggData: { baseHours: 30 },
    });

    const api = createMockApi({
      columns: [nameCol, hoursCol],
      rowGroupCount: 1,
      nodes: [groupHeader, leaf1, leaf2, groupFooter, grandTotal],
      getValue: (col, node) => {
        const id = col.getColId();
        if (node.group) {
          if (id === 'baseHours') return (node as { aggData?: Record<string, unknown> }).aggData?.baseHours;
          // group column empty on footer (как часто в AG Grid)
          if (node.footer) return null;
          return node.key;
        }
        return node.data?.[id];
      },
    });

    const { rows } = collectGridExportData(api, { includeGroups: true });
    expect(rows).toEqual([
      ['Объект-1', '30'],
      ['Строка 1', '10'],
      ['Строка 2', '20'],
      ['Итого: Объект-1', '30'],
      ['Итого', '30'],
    ]);
  });

  it('excludes selection checkbox column', () => {
    const selectCol = {
      getColId: () => 'ag-Grid-SelectionColumn',
      getColDef: () => ({ checkboxSelection: true, headerName: '' }),
    } as unknown as Column;
    const leaf = mockLeaf({ facilityName: 'A', baseHours: 1 });
    const api = createMockApi({
      columns: [selectCol, nameCol, hoursCol],
      nodes: [leaf],
      getValue: (col, node) => node.data?.[col.getColId()],
    });

    const { headers, rows } = collectGridExportData(api);
    expect(headers).toEqual(['Объект', 'Базовый']);
    expect(rows[0]).toHaveLength(2);
  });
});
