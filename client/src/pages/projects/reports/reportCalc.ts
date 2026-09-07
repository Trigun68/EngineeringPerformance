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

/** Короткие подписи чипов (мокап ReportRedesignMockup). */
export const INTERPRETATION_SHORT_RU: Record<Interpretation, string> = {
  Excellent: 'Отлично',
  Good: 'Хорошо',
  Satisfactory: 'Удовлетворительно',
  NeedsAttention: 'Требует внимания',
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

/** Объём поддерева (L1 после rollup часов) либо реконструкция hours/productivity. */
export function resolveSubtreePhysicalVolume(
  row: Pick<
    PerformanceRow,
    'subtreePhysicalVolume' | 'factPhysicalVolume' | 'factLaborHours' | 'factProductivity'
  >,
): number | null {
  if (row.subtreePhysicalVolume != null && row.subtreePhysicalVolume > 0) {
    return row.subtreePhysicalVolume;
  }
  const hours = row.factLaborHours;
  const productivity = row.factProductivity;
  if (hours != null && hours > 0 && productivity != null && productivity > 0) {
    return hours / productivity;
  }
  return row.factPhysicalVolume ?? null;
}

/** На корне IIC — объём поддерева; на потомках — собственный объём. */
export function displayPhysicalVolume(
  row: Pick<
    PerformanceRow,
    'parentId' | 'subtreePhysicalVolume' | 'factPhysicalVolume' | 'factLaborHours' | 'factProductivity'
  >,
): number | null {
  if (row.parentId != null) {
    return row.factPhysicalVolume ?? null;
  }
  return resolveSubtreePhysicalVolume(row);
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

/** Выводы по группам IIC первого уровня (часы факта после rollup). */
export function buildInsights(
  firstLevel: PerformanceTreeRow[],
  summary: ProjectVersionAnalysisSummary,
  accuracyScore: number | null,
): Insight[] {
  const insights: Insight[] = [];
  const scored = firstLevel.filter((r) => r.zScore !== null).sort((a, b) => b.zScore! - a.zScore!);

  for (const row of scored.filter((r) => r.zScore! > 1)) {
    const volume = resolveSubtreePhysicalVolume(row);
    const overrun =
      row.factLaborHours && row.normProductivity && volume
        ? row.factLaborHours - row.normProductivity * volume
        : null;
    insights.push({
      kind: 'risk',
      title: `${row.workDirectionName}: Z = ${fmtSigned(row.zScore)}`,
      detail: `Перерасход относительно нормы IIC${
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
        'Часть характеристик проекта слабо представлена в выборке IIC. Итоговую Z-оценку следует трактовать как ориентир, а не как приговор по производительности.',
    });
  }

  return insights;
}

export function keyDisciplines(firstLevel: PerformanceTreeRow[], top = 3): PerformanceTreeRow[] {
  return firstLevel
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
  optimisticZScore: number | null;
  optimisticHours: number | null;
  baseZScore: number | null;
  baseHours: number | null;
  pessimisticZScore: number | null;
  pessimisticHours: number | null;
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
 * SoT — client/src/constants/planFactColors.ts; здесь реэкспорт для обратной совместимости.
 */
export { PLAN_ACCENT_COLOR as ACCENT_PLAN, FACT_ACCENT_COLOR as ACCENT_FACT } from '../../../constants/planFactColors';

/** Отклонение факта от плана в процентах: (fact − plan) / plan × 100; null при отсутствии данных. */
export function hoursDeltaPct(planHours: number | null, factHours: number | null): number | null {
  if (planHours === null || planHours === undefined || planHours === 0) return null;
  if (factHours === null || factHours === undefined) return null;
  return ((factHours - planHours) / planHours) * 100;
}

/** Диапазон полосы Z (мокап и «Сводный вер 2»). */
export const Z_SCALE_MIN = -3;
export const Z_SCALE_MAX = 3;

/**
 * Позиция маркера на шкале: слева плохо (больший Z), справа хорошо (меньший Z).
 * left% = (MAX − z) / (MAX − MIN).
 */
export function zScaleVisualPct(z: number, min = Z_SCALE_MIN, max = Z_SCALE_MAX): number {
  const clamped = Math.min(max, Math.max(min, z));
  return ((max - clamped) / (max - min)) * 100;
}

/** Зоны слева направо: Критично → Отлично. Ширины — реальные пороги ±1 / ±0.5. */
export const Z_SCALE_ZONES: ReadonlyArray<{
  from: number;
  to: number;
  label: string;
  interpretation: Interpretation;
}> = [
  { from: 1, to: Z_SCALE_MAX, label: 'Критично', interpretation: 'Critical' },
  { from: 0.5, to: 1, label: 'Внимание', interpretation: 'NeedsAttention' },
  { from: -0.5, to: 0.5, label: 'Удовл.', interpretation: 'Satisfactory' },
  { from: -1, to: -0.5, label: 'Хорошо', interpretation: 'Good' },
  { from: Z_SCALE_MIN, to: -1, label: 'Отлично', interpretation: 'Excellent' },
];

/** Подпись источника карточки «План»: «из версии {name} (основная)». */
export function formatPlanSourceLabel(
  versionName: string | null | undefined,
  isPrimary: boolean | null | undefined,
): string {
  if (!versionName) return 'Нет версии плана';
  return isPrimary === false ? `из версии ${versionName}` : `из версии ${versionName} (основная)`;
}

/** Подпись источника карточки «Факт»: «{name} (основная)». */
export function formatFactSourceLabel(
  versionName: string | null | undefined,
  isPrimary: boolean | null | undefined,
): string {
  if (!versionName) return '';
  return isPrimary === false ? versionName : `${versionName} (основная)`;
}

export interface VersionHistoryLike {
  versionId: number;
  versionName: string;
  createdAt: string;
  direction: number;
  weightedZScore: number | null;
  totalLaborHours: number | null;
  optimisticZScore?: number | null;
  optimisticLaborHours?: number | null;
  baseZScore?: number | null;
  baseLaborHours?: number | null;
  pessimisticZScore?: number | null;
  pessimisticLaborHours?: number | null;
}

/** Дата оси динамики: «10 фев», «4 мар». */
export function formatTrendAxisDate(date: Date): string {
  return date.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' }).replaceAll('.', '');
}

export function historyToTrendPoints(history: VersionHistoryLike[]): VersionTrendPoint[] {
  return history.map((h) => {
    const createdAt = h.createdAt ? new Date(h.createdAt) : null;
    return {
      versionId: h.versionId,
      versionName: h.versionName,
      direction: h.direction,
      date: createdAt && !Number.isNaN(createdAt.getTime()) ? formatTrendAxisDate(createdAt) : null,
      sortDate: createdAt && !Number.isNaN(createdAt.getTime()) ? createdAt.getTime() : 0,
      weightedZScore: h.weightedZScore,
      totalHours: h.totalLaborHours,
      optimisticZScore: h.optimisticZScore ?? null,
      optimisticHours: h.optimisticLaborHours ?? null,
      baseZScore: h.baseZScore ?? null,
      baseHours: h.baseLaborHours ?? null,
      pessimisticZScore: h.pessimisticZScore ?? null,
      pessimisticHours: h.pessimisticLaborHours ?? null,
    };
  });
}

/** Подпись точки графика динамики: Z и трудоёмкость. */
export function formatTrendPointLabel(z: number | null | undefined, hours: number | null | undefined): string {
  return `${fmtSigned(z)} · ${fmt(hours, 0)} ч`;
}

function scenarioTooltipLine(label: string, z: number | null | undefined, hours: number | null | undefined): string {
  return `${label}: Z ${fmtSigned(z)} · ${fmt(hours, 0)} ч`;
}

/** Строки подсказки: три сценария планирования (оптимист / база / пессимист). */
export function formatTrendScenarioTooltipLines(point: Pick<
  VersionTrendPoint,
  'versionName' | 'optimisticZScore' | 'optimisticHours' | 'baseZScore' | 'baseHours' | 'pessimisticZScore' | 'pessimisticHours'
>): string[] {
  return [
    point.versionName,
    scenarioTooltipLine('Оптимистичный', point.optimisticZScore, point.optimisticHours),
    scenarioTooltipLine('Базовый', point.baseZScore, point.baseHours),
    scenarioTooltipLine('Пессимистичный', point.pessimisticZScore, point.pessimisticHours),
  ];
}

export function formatTrendScenarioTooltipHtml(point: VersionTrendPoint): string {
  const [title, ...lines] = formatTrendScenarioTooltipLines(point);
  return [`<b>${title}</b>`, ...lines].join('<br/>');
}

/**
 * Точки графика мокапа: все Plan с Z, из Fact — только основная версия.
 * Сортировка по дате создания, затем по id.
 */
export function selectVersionTrendPoints(
  points: VersionTrendPoint[],
  primaryFactVersionId: number | null,
): VersionTrendPoint[] {
  return points
    .filter((p) => {
      if (p.weightedZScore === null) return false;
      if (p.direction === DIRECTION_PLAN) return true;
      if (p.direction === DIRECTION_FACT) {
        return primaryFactVersionId !== null && p.versionId === primaryFactVersionId;
      }
      return false;
    })
    .sort((a, b) => a.sortDate - b.sortDate || a.versionId - b.versionId);
}
