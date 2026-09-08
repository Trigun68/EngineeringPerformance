import type { ProductivityTrendTransition } from '../../../api/productivityTrendApi';

export const VERDICT_LABEL: Record<string, string> = {
  tightening: 'Ужесточение',
  loosening: 'Ослабление',
  ambiguous: 'Неоднозначно',
  insufficient: 'Недостаточно данных',
  stable: 'Без изменения',
};

export const VERDICT_COLOR: Record<string, 'warning' | 'success' | 'default'> = {
  tightening: 'warning',
  loosening: 'success',
  ambiguous: 'default',
  insufficient: 'default',
  stable: 'default',
};

const VERDICT_PRIORITY: Record<string, number> = {
  tightening: 0,
  loosening: 1,
  ambiguous: 2,
  insufficient: 3,
  stable: 4,
};

const FORMULA_LABEL: Record<string, string> = {
  Linear: 'Линейная',
  Logarithmic: 'Логарифмическая',
  ShiftedLog: 'Сдвинутая логарифмическая',
  Wright: 'Уайт',
  Power: 'Степенная',
  ExponentialDecay: 'Эксп. затухание',
  PiecewiseLinear: 'Кусочно-линейная',
  Polynomial: 'Полиномиальная',
  Constant: 'Постоянная',
  Hyperbolic: 'Гиперболическая',
  Exponential: 'Экспоненциальная',
};

export function sortByAttention(transitions: ProductivityTrendTransition[]): ProductivityTrendTransition[] {
  return [...transitions].sort(
    (a, b) => (VERDICT_PRIORITY[a.verdict] ?? 99) - (VERDICT_PRIORITY[b.verdict] ?? 99),
  );
}

export function formatNum(value: number | null | undefined, digits = 2): string {
  if (value == null || Number.isNaN(value)) return '—';
  return value.toLocaleString('ru-RU', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

export function pair(oldValue: number | null, newValue: number | null): string {
  return `${formatNum(oldValue)} → ${formatNum(newValue)}`;
}

export function formulaLabel(type: string): string {
  return FORMULA_LABEL[type] ?? type;
}
