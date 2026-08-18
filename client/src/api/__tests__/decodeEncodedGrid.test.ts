import { describe, expect, it } from 'vitest';
import { decodeEncodedGrid, type EncodedGrid } from '../decodeEncodedGrid';

describe('decodeEncodedGrid', () => {
  it('подставляет значения из словаря по индексу', () => {
    const encoded: EncodedGrid = {
      columns: ['rowId', 'catalogName', 'quantity'],
      strings: { catalogName: ['Бетон', 'Кирпич'] },
      rows: [
        [1, 0, 10.5],
        [2, 1, 20],
        [3, 0, 30],
      ],
    };

    expect(decodeEncodedGrid(encoded)).toEqual([
      { rowId: 1, catalogName: 'Бетон', quantity: 10.5 },
      { rowId: 2, catalogName: 'Кирпич', quantity: 20 },
      { rowId: 3, catalogName: 'Бетон', quantity: 30 },
    ]);
  });

  it('оставляет как есть колонки без словаря', () => {
    const encoded: EncodedGrid = {
      columns: ['catalogCode', 'isEquipment', 'zScore'],
      strings: {},
      rows: [['04-04-002-05', false, null]],
    };

    expect(decodeEncodedGrid(encoded)).toEqual([
      { catalogCode: '04-04-002-05', isEquipment: false, zScore: null },
    ]);
  });

  it('не путает числовую колонку со словарной', () => {
    // rowId — число, но словаря для него нет: значение не должно уехать в индекс.
    const encoded: EncodedGrid = {
      columns: ['rowId', 'tag'],
      strings: { tag: ['a', 'b'] },
      rows: [[1, 1]],
    };

    expect(decodeEncodedGrid(encoded)).toEqual([{ rowId: 1, tag: 'b' }]);
  });

  it('переживает null в словарной колонке', () => {
    const encoded: EncodedGrid = {
      columns: ['catalogName'],
      strings: { catalogName: ['Бетон'] },
      rows: [[null], [0]],
    };

    expect(decodeEncodedGrid(encoded)).toEqual([{ catalogName: null }, { catalogName: 'Бетон' }]);
  });

  it('возвращает пустой массив на пустом или отсутствующем гриде', () => {
    expect(decodeEncodedGrid(undefined)).toEqual([]);
    expect(decodeEncodedGrid(null)).toEqual([]);
    expect(decodeEncodedGrid({ columns: ['a'], strings: {}, rows: [] })).toEqual([]);
  });
});
