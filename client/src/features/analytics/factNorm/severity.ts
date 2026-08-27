import type { NormComparison } from '../../../api/factNormAnalyticsApi';

/**
 * Оценка «серьёзности» отклонения факта от нормы в единицах σ нормы (либо σ факта,
 * если у нормы σ не задана). Чем больше значение, тем сильнее группа выбивается из
 * допуска и тем выше приоритет пересмотра нормы.
 */
export function severityRatio(row: NormComparison): number {
  const sigma = row.normStdDeviation > 0 ? row.normStdDeviation : row.factStdDeviation;
  if (!sigma || sigma <= 0) {
    // σ неизвестна — ориентируемся на вердикт алгоритма как на приблизительную серьёзность.
    if (row.verdict === 'ChangeFormulaType') return 2;
    if (row.verdict === 'AdjustParameters') return 1;
    return 0;
  }
  return Math.abs(row.deltaMean) / sigma;
}

export type SeverityLevel = 'low' | 'medium' | 'high';

export function severityLevel(ratio: number): SeverityLevel {
  if (ratio >= 1.5) return 'high';
  if (ratio >= 0.5) return 'medium';
  return 'low';
}

const SEVERITY_SOLID: Record<SeverityLevel, string> = {
  low: '#27AE60',
  medium: '#F39C12',
  high: '#E74C3C',
};

const SEVERITY_BG: Record<SeverityLevel, string> = {
  low: 'rgba(39, 174, 96, 0.14)',
  medium: 'rgba(243, 156, 18, 0.16)',
  high: 'rgba(231, 76, 60, 0.18)',
};

const SEVERITY_LABEL: Record<SeverityLevel, string> = {
  low: 'В допуске',
  medium: 'Есть отклонение',
  high: 'Требует пересмотра',
};

export function severityColor(ratio: number): string {
  return SEVERITY_SOLID[severityLevel(ratio)];
}

export function severityBackground(ratio: number): string {
  return SEVERITY_BG[severityLevel(ratio)];
}

export function severityLabel(ratio: number): string {
  return SEVERITY_LABEL[severityLevel(ratio)];
}

/** Цвет фона ячейки таблицы для дельта-показателей (Δμ, Δσ, ΔR²), пропорциональный |value| / scale. */
export function deltaHeatBackground(value: number | null | undefined, scale: number): string {
  if (value == null || !Number.isFinite(value) || scale <= 0) return 'transparent';
  return severityBackground(Math.abs(value) / scale);
}
