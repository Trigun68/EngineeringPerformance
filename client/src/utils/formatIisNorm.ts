export interface IisNormDisplayItem {
  code?: string | null;
  name: string;
  unitName?: string | null;
}

/** Формат классификатора IIC: «код наименование (ед.)». */
export function formatIisNorm(item: { code?: string | null; name: string; unitName?: string | null }): string {
  const codePart = item.code?.trim() ? `${item.code.trim()} ` : '';
  const base = `${codePart}${item.name}`.trim();
  const unit = item.unitName?.trim();
  return unit ? `${base} (${unit})` : base;
}

/** Удобная обёртка для valueFormatter в AG Grid. */
export function formatAsIisNorm(
  code: string | null | undefined,
  name: string,
  unit: string | null | undefined,
): string {
  return formatIisNorm({ code, name, unitName: unit });
}
