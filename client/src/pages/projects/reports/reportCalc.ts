import type { PerformanceRow, ProjectVersionAnalysisSummary } from '../../../api/analyticsApi';

/** Паритет backend enum PerformanceInterpretation (Domain/Enums/PerformanceInterpretation.cs). */
export type Interpretation = 'Excellent' | 'Good' | 'Satisfactory' | 'NeedsAttention' | 'Critical';

/**
 * Пороги идентичны PerformanceInterpretationHelper.GetInterpretation на backend.
 * Интерпретация всегда пересчитывается на клиенте из zScore, а не берётся из
 * API-поля interpretation — оно сериализуется как enum-число (без JsonStringEnumConverter).
 */
export function getInterpretation(z: number | null | undefined): Interpretation | null {
  if (z === null || z === undefined || Number.isNaN(z)) return null;
  if (z < -1) return 'Excellent';
  if (z < -0.5) return 'Good';
  if (z <= 0.5) return 'Satisfactory';
  if (z <= 1) return 'NeedsAttention';
  return 'Critical';
}

export const INTERPRETATION_RU: Record<Interpretation, string> = {
  Excellent: 'Отличная производительность',
  Good: 'Хорошая производительность',
  Satisfactory: 'Удовлетворительная производительность',
  NeedsAttention: 'Требует внимания',
  Critical: 'Критически низкая производительность',
};

export const INTERPRETATION_SHORT_RU: Record<Interpretation, string> = {
  Excellent: 'Отлично',
  Good: 'Хорошо',
  Satisfactory: 'Удовлетв.',
  NeedsAttention: 'Внимание',
  Critical: 'Критично',
};

/**
 * Палитра интерпретации — дизайн-мокап ReportRedesignMockup (ветка v0):
 * смягчённый красный, более выразительный янтарный для «удовлетворительно».
 */
export const INTERPRETATION_COLOR: Record<Interpretation, string> = {
  Excellent: '#0F9D6B',
  Good: '#2F9E6E',
  Satisfactory: '#C77A1F',
  NeedsAttention: '#D2691E',
  Critical: '#C6483F',
};

/** Фоны чипов интерпретации (мокап) — приглушённые оттенки основного цвета. */
export const INTERPRETATION_BG: Record<Interpretation, string> = {
  Excellent: '#E5F7EF',
  Good: '#EAF6F0',
  Satisfactory: '#FBF0E1',
  NeedsAttention: '#FCEEE3',
  Critical: '#F8E9E7',
};

/** Пастельные цвета зон полосы шкалы Z (мокап) — светлее INTERPRETATION_COLOR. */
export const ZONE_COLOR: Record<Interpretation, string> = {
  Excellent: '#6FCB9F',
  Good: '#A8DDBE',
  Satisfactory: '#F4CF62',
  NeedsAttention: '#F0C48A',
  Critical: '#DDA9A4',
};

export const MUTED_COLOR = '#7B8794';

export function interpretationColor(z: number | null | undefined): string {
  const i = getInterpretation(z);
  return i ? INTERPRETATION_COLOR[i] : MUTED_COLOR;
}

export function fmt(value: number | null | undefined, digits = 2): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return value.toLocaleString('ru-RU', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

export function fmtSigned(value: number | null | undefined, digits = 2): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return (value > 0 ? '+' : '') + fmt(value, digits);
}

/** Дерево направлений, собранное из плоского PerformanceRow[] (агрегаты уже рассчитаны backend'ом). */
export interface PerformanceTreeRow extends PerformanceRow {
  children: PerformanceTreeRow[];
}

export function buildTree(rows: PerformanceRow[]): PerformanceTreeRow[] {
  const byId = new Map<number, PerformanceTreeRow>();
  rows.forEach((r) => byId.set(r.workDirectionId, { ...r, children: [] }));

  const roots: PerformanceTreeRow[] = [];
  byId.forEach((node) => {
    const parent = node.parentId !== null ? byId.get(node.parentId) : undefined;
    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  });
  return roots;
}

export function flattenTree(rows: PerformanceTreeRow[]): PerformanceTreeRow[] {
  return rows.flatMap((r) => [r, ...flattenTree(r.children)]);
}

export type InsightKind = 'risk' | 'attention' | 'strength' | 'data';

export interface Insight {
  kind: InsightKind;
  title: string;
  detail: string;
}

/** Порт lib/performance/insights.ts из прототипа (без поля «ответственный» — в реальных данных его нет). */
export function buildInsights(
  leaves: PerformanceTreeRow[],
  summary: ProjectVersionAnalysisSummary,
  accuracyScore: number | null,
): Insight[] {
  const insights: Insight[] = [];
  const scored = leaves.filter((r) => r.zScore !== null).sort((a, b) => b.zScore! - a.zScore!);

  for (const row of scored.filter((r) => r.zScore! > 1)) {
    const overrun =
      row.factLaborHours && row.normProductivity && row.factPhysicalVolume
        ? row.factLaborHours - row.normProductivity * row.factPhysicalVolume
        : null;
    insights.push({
      kind: 'risk',
      title: `${row.workDirectionName}: Z = ${fmtSigned(row.zScore)}`,
      detail: `Перерасход относительно нормы ИИЦ${
        overrun !== null ? ` составляет ${fmt(overrun, 0)} ч` : ''
      }. Требуется разбор причин: пересмотреть состав работ и проверить полноту исходных данных.`,
    });
  }

  for (const row of scored.filter((r) => r.zScore! > 0.5 && r.zScore! <= 1)) {
    insights.push({
      kind: 'attention',
      title: `${row.workDirectionName}: Z = ${fmtSigned(row.zScore)}`,
      detail:
        'Показатель выше нормы, но в пределах одного σ. Поставить направление на контроль до следующей версии факта.',
    });
  }

  const best = scored.filter((r) => r.zScore! < -0.5).reverse();
  if (best.length > 0) {
    insights.push({
      kind: 'strength',
      title: `Опережение нормы: ${best.map((r) => r.workDirectionName).join(', ')}`,
      detail:
        'Практики этих направлений стоит зафиксировать как референс и учесть при актуализации внутренних норм трудоёмкости.',
    });
  }

  const totalFactLaborHours = summary.totalFactLaborHours ?? 0;
  const unallocatedLaborHours = summary.unallocatedLaborHours ?? 0;
  const unallocatedShare = totalFactLaborHours > 0 ? (unallocatedLaborHours / totalFactLaborHours) * 100 : 0;
  if (unallocatedLaborHours > 0) {
    insights.push({
      kind: 'data',
      title: `Не отнесено к направлениям: ${fmt(unallocatedLaborHours, 0)} ч (${fmt(unallocatedShare, 1)}%)`,
      detail:
        unallocatedShare > 2
          ? 'Доля нераспределённых часов искажает средневзвешенную Z-оценку. Требуется доразметка строк трудоёмкости при следующем импорте.'
          : 'Доля нераспределённых часов в допустимых пределах и существенно не влияет на итоговую Z-оценку.',
    });
  }

  if (accuracyScore !== null && accuracyScore < 85) {
    insights.push({
      kind: 'data',
      title: `Точность сравнения: ${fmt(accuracyScore, 1)}%`,
      detail:
        'Часть характеристик проекта слабо представлена в выборке ИИЦ. Итоговую Z-оценку следует трактовать как ориентир, а не как приговор по производительности.',
    });
  }

  return insights;
}

export function keyDisciplines(leaves: PerformanceTreeRow[], top = 3): PerformanceTreeRow[] {
  return leaves
    .filter((r) => (r.subtreeHours ?? 0) > 0)
    .sort((a, b) => b.subtreeHours! - a.subtreeHours!)
    .slice(0, top);
}

export interface VersionTrendPoint {
  versionId: number;
  versionName: string;
  direction: number;
  date: string | null;
  sortDate: number;
  weightedZScore: number | null;
  totalHours: number | null;
}

export const DIRECTION_LABEL: Record<number, string> = {
  1: 'Версия плана',
  2: 'Версия факта',
  3: 'Прогноз',
};

/** Направления версии — паритет backend enum AnalysisDirection. */
export const DIRECTION_PLAN = 1;
export const DIRECTION_FACT = 2;

/**
 * Акцентные цвета плана и факта (мокап ReportRedesignMockup):
 * план — фиолетовый (менее акцентный), факт — синий (основной акцент, крупный маркер).
 */
export const ACCENT_PLAN = '#8E6FD8';
export const ACCENT_FACT = '#2F6FE0';

/** Отклонение факта от плана в процентах: (fact − plan) / plan × 100; null при отсутствии данных. */
export function hoursDeltaPct(planHours: number | null, factHours: number | null): number | null {
  if (planHours === null || planHours === undefined || planHours === 0) return null;
  if (factHours === null || factHours === undefined) return null;
  return ((factHours - planHours) / planHours) * 100;
}
