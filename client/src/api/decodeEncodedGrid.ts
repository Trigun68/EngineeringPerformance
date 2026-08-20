/**
 * Распаковка гридов «Детально» из колоночного формата (ISS-019).
 *
 * Сервер отдаёт имена полей один раз, повторяющиеся строки — словарём, а в ячейке оставляет
 * индекс. Здесь формат разворачивается обратно в обычные объекты строк, поэтому гриды и
 * колонки работают с тем же контрактом, что и раньше, и про кодирование ничего не знают.
 */
export interface EncodedGrid {
  columns: string[];
  /** Словари значений для колонок, где ячейка — индекс в массиве. */
  strings: Record<string, string[]>;
  rows: (string | number | boolean | null)[][];
}

export function decodeEncodedGrid<T>(encoded: EncodedGrid | T[] | null | undefined): T[] {
  if (Array.isArray(encoded)) {
    return encoded;
  }
  if (!encoded?.columns) {
    return [];
  }

  const { columns, rows } = encoded;
  const strings = encoded.strings ?? {};
  // Таблица на колонку по индексу: в цикле по 30k строк это дешевле, чем поиск по имени.
  const tables = columns.map((name) => strings[name]);

  const result: T[] = new Array(rows.length);
  for (let r = 0; r < rows.length; r += 1) {
    const cells = rows[r];
    const row: Record<string, unknown> = {};
    for (let c = 0; c < columns.length; c += 1) {
      const table = tables[c];
      const cell = cells[c];
      row[columns[c]] = table !== undefined && typeof cell === 'number' ? table[cell] : cell;
    }
    result[r] = row as T;
  }

  return result;
}
