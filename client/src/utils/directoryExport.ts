import type { GridApi } from 'ag-grid-community';

function formatCellValue(value: unknown): string {
  if (value == null) return '';
  if (typeof value === 'boolean') return value ? 'Да' : 'Нет';
  if (value instanceof Date) {
    const d = value.getDate().toString().padStart(2, '0');
    const m = (value.getMonth() + 1).toString().padStart(2, '0');
    return `${d}.${m}.${value.getFullYear()}`;
  }
  return String(value);
}

/** Собирает видимые колонки и отфильтрованные строки AG Grid для экспорта в Excel. */
export function collectGridExportData(api: GridApi): { headers: string[]; rows: string[][] } {
  const columns = api.getAllDisplayedColumns()?.filter((col) => {
    const def = col.getColDef();
    return !def.checkboxSelection && col.getColId() !== 'ag-Grid-SelectionColumn';
  }) ?? [];

  const headers = columns.map((col) => {
    const def = col.getColDef();
    return def.headerName ?? def.field ?? col.getColId();
  });

  const rows: string[][] = [];
  api.forEachNodeAfterFilterAndSort((node) => {
    if (node.group || !node.data) return;
    const cells = columns.map((col) => {
      const value = api.getValue(col, node);
      return formatCellValue(value);
    });
    rows.push(cells);
  });

  return { headers, rows };
}

export function buildExportFileName(tabName: string): string {
  const now = new Date();
  const pad = (n: number) => n.toString().padStart(2, '0');
  const stamp = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  const safe = tabName.replace(/[/\\?%*:|"<>]/g, '_').trim() || 'Справочник';
  const cleaned = safe.replace(/^_+$/, 'Справочник');
  return `${cleaned}_${stamp}.xlsx`;
}
