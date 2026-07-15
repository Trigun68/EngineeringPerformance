import { describe, expect, it } from 'vitest';
import { resolveIicDirectionUnitId } from '../resolveIicDirectionUnit';
import type { DictionaryItem } from '../../types/dictionary';

const directions: DictionaryItem[] = [
  { id: 1, name: 'Root', code: 'R', parentId: null, unitId: 10 },
  { id: 2, name: 'Child', code: 'C', parentId: 1, unitId: null },
  { id: 3, name: 'Leaf', code: 'L', parentId: 2, unitId: 5 },
  { id: 4, name: 'Orphan', code: 'O', parentId: null, unitId: 7 },
];

describe('resolveIicDirectionUnitId', () => {
  it('inherits unit from ancestor when node has no own unit', () => {
    expect(resolveIicDirectionUnitId(2, directions)).toBe(10);
  });

  it('prefers ancestor unit over own unit when ancestor defines one', () => {
    expect(resolveIicDirectionUnitId(3, directions)).toBe(10);
  });

  it('uses own unit when no ancestor defines one', () => {
    expect(resolveIicDirectionUnitId(4, directions)).toBe(7);
  });

  it('returns null for unknown direction', () => {
    expect(resolveIicDirectionUnitId(999, directions)).toBeNull();
  });
});
