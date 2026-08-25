/**
 * Строки прогноза без нормы и без рассчитанных часов — нужен fallback PLI или норма в снимке версии.
 * Учитываются только направления ИИС (workDirectionId ≥ 0).
 * Плейсхолдер «(пусто)» / нераспределённое — отдельно: там важны только позиции с «Оценка = да».
 */
export interface NormFallbackWarningRow {
  workDirectionId: number;
  name: string;
  plannedVolume?: number | null;
  factPhysicalVolume?: number | null;
  normProductivity?: number | null;
  baseHours?: number | null;
}

export interface AssessmentDetailWarningRow {
  isAssessment?: string | null;
  classifierId?: number | null;
  quantity?: number | null;
}

export function collectNormFallbackWarnings(rows: NormFallbackWarningRow[]): string[] {
  const warnings: string[] = [];
  const seen = new Set<number>();

  for (const row of rows) {
    // «(пусто)» / нераспределённое — не про отсутствие нормы в снимке.
    if (row.workDirectionId < 0) continue;
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

/**
 * Позиции с «Оценка = да» без классификатора ИИС.
 * «Оценка = нет» — осознанный выбор, в предупреждение не входят.
 */
export function collectMissingClassifierWarnings(rows: AssessmentDetailWarningRow[]): string[] {
  let count = 0;
  let volume = 0;

  for (const row of rows) {
    if (row.isAssessment !== 'Да') continue;
    if (row.classifierId != null) continue;
    const qty = row.quantity ?? 0;
    if (qty <= 0) continue;
    count += 1;
    volume += qty;
  }

  if (count === 0) return [];

  const volLabel = Number.isInteger(volume) ? String(volume) : volume.toFixed(3).replace(/\.?0+$/, '');
  return [
    `${count} позиц. с «Оценка = да» без классификатора IIC (объём ${volLabel}). Позиции с «Оценка = нет» не учитываются.`,
  ];
}

export function collectForecastVolumeWarnings(
  summaryRows: NormFallbackWarningRow[],
  detailRows: AssessmentDetailWarningRow[],
): string[] {
  return [
    ...collectNormFallbackWarnings(summaryRows),
    ...collectMissingClassifierWarnings(detailRows),
  ];
}
