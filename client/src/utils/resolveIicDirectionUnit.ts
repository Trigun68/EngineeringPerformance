import type { DictionaryItem } from '../types/dictionary';

/** Наследование ед. изм. группы IIC: сначала у предков вверх, иначе у самой группы (как WPF). */
export function resolveIicDirectionUnitId(
  directionId: number,
  directions: DictionaryItem[],
): number | null {
  const byId = new Map(directions.map((d) => [d.id, d]));
  const direction = byId.get(directionId);
  if (!direction) return null;

  if (direction.parentId != null && byId.has(direction.parentId)) {
    let current = byId.get(direction.parentId)!;
    while (true) {
      if (current.unitId != null) return current.unitId;
      if (current.parentId == null || !byId.has(current.parentId)) break;
      current = byId.get(current.parentId)!;
    }
  }

  return direction.unitId ?? null;
}

export function resolveIicDirectionUnitName(
  directionId: number,
  directions: DictionaryItem[],
  getUnitName: (id: number | null | undefined) => string,
): string {
  const unitId = resolveIicDirectionUnitId(directionId, directions);
  return unitId != null ? getUnitName(unitId) : '';
}
