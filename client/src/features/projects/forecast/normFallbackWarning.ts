/**
 * Строки прогноза без нормы и без рассчитанных часов — нужен fallback PLI или норма в снимке версии.
 */
export interface NormFallbackWarningRow {
  workDirectionId: number;
  name: string;
  plannedVolume?: number | null;
  factPhysicalVolume?: number | null;
  normProductivity?: number | null;
  baseHours?: number | null;
}

export function collectNormFallbackWarnings(rows: NormFallbackWarningRow[]): string[] {
  const warnings: string[] = [];
  const seen = new Set<number>();

  for (const row of rows) {
    if (seen.has(row.workDirectionId)) continue;

    const volume = row.plannedVolume ?? row.factPhysicalVolume ?? 0;
    const hasNorm = row.normProductivity != null && row.normProductivity > 0;
    const hasHours = (row.baseHours ?? 0) > 0;

    if (volume > 0 && !hasNorm && !hasHours) {
      seen.add(row.workDirectionId);
      warnings.push(
        `«${row.name}»: объём ${volume}, но нет нормы в снимке версии и нет часов PLI — прогноз может быть нулевым.`,
      );
    }
  }

  return warnings;
}
