/** Формат отображения Z в полях параметров планирования. */
export function formatZDisplay(value: number): string {
  return Number.isFinite(value) ? value.toFixed(2).replace('.', ',') : '0,00';
}

/** Парсит Z с точки или запятой; незавершённый ввод («», «-», «,») → null. */
export function parseZInput(raw: string): number | null {
  const normalized = raw.trim().replace(/\s/g, '').replace(',', '.');
  if (
    normalized === ''
    || normalized === '-'
    || normalized === '.'
    || normalized === '-.'
  ) {
    return null;
  }
  const value = Number(normalized);
  return Number.isFinite(value) ? value : null;
}

export type ZDraftTriple = { zPessimistic: string; zBase: string; zOptimistic: string };

export function draftsFromRows(
  rows: Array<{ iicWorkDirectionId: number; zPessimistic: number; zBase: number; zOptimistic: number }>,
): Record<number, ZDraftTriple> {
  return Object.fromEntries(
    rows.map((row) => [
      row.iicWorkDirectionId,
      {
        zPessimistic: formatZDisplay(row.zPessimistic),
        zBase: formatZDisplay(row.zBase),
        zOptimistic: formatZDisplay(row.zOptimistic),
      },
    ]),
  );
}
