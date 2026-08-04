/** Краткое имя справочника объёмов: часть до первого двоеточия. */
export function shortCatalogName(full: string | null | undefined): string | null {
  if (full == null) return null;
  const trimmed = full.trim();
  if (!trimmed) return null;
  const beforeColon = trimmed.split(':')[0]?.trim() ?? '';
  return beforeColon || null;
}
