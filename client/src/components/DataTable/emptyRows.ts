/**
 * Стабильный пустой массив строк для AG Grid / DataTable.
 * Inline `[]` в пропах даёт новую identity каждый рендер.
 */
const EMPTY: never[] = [];
Object.freeze(EMPTY);

export const EMPTY_ROWS: never[] = EMPTY;

export function emptyRows<T>(): T[] {
  return EMPTY as T[];
}
