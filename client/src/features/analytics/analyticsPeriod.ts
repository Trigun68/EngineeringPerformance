/** Пресеты периода для страницы «Аналитика». */
export type AnalyticsPeriodPreset =
  | '1m'
  | 'quarter'
  | 'prev-quarter'
  | 'year'
  | 'prev-year'
  | 'custom';

export const ANALYTICS_PERIOD_PRESET_LABELS: Record<AnalyticsPeriodPreset, string> = {
  '1m': '1 месяц',
  quarter: 'Текущий квартал',
  'prev-quarter': 'Предыдущий квартал',
  year: 'Текущий год',
  'prev-year': 'Предыдущий год',
  custom: 'Произвольный период',
};

export const ANALYTICS_PERIOD_PRESETS: AnalyticsPeriodPreset[] = [
  '1m',
  'quarter',
  'prev-quarter',
  'year',
  'prev-year',
  'custom',
];

export interface DateRange {
  dateFrom: string;
  dateTo: string;
}

function pad2(n: number): string {
  return String(n).padStart(2, '0');
}

/** Локальная дата → yyyy-MM-dd. */
export function formatIsoDate(d: Date): string {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

function startOfDay(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

function addDays(d: Date, days: number): Date {
  const next = new Date(d);
  next.setDate(next.getDate() + days);
  return next;
}

function quarterStartMonth(monthIndex: number): number {
  return Math.floor(monthIndex / 3) * 3;
}

/**
 * Рассчитывает диапазон дат для пресета.
 * Для `custom` возвращает пустые строки (значения задаёт UI).
 */
export function resolveAnalyticsPeriod(
  preset: AnalyticsPeriodPreset,
  today: Date = new Date(),
): DateRange {
  if (preset === 'custom') {
    return { dateFrom: '', dateTo: '' };
  }

  const end = startOfDay(today);
  const y = end.getFullYear();
  const m = end.getMonth();

  switch (preset) {
    case '1m': {
      // Последние 30 дней, включая сегодня (29 дней назад … сегодня).
      const from = addDays(end, -29);
      return { dateFrom: formatIsoDate(from), dateTo: formatIsoDate(end) };
    }
    case 'quarter': {
      const qStart = new Date(y, quarterStartMonth(m), 1);
      return { dateFrom: formatIsoDate(qStart), dateTo: formatIsoDate(end) };
    }
    case 'prev-quarter': {
      const thisQStartMonth = quarterStartMonth(m);
      const prevQStart = new Date(y, thisQStartMonth - 3, 1);
      const prevQEnd = new Date(y, thisQStartMonth, 0); // последний день предыдущего квартала
      return { dateFrom: formatIsoDate(prevQStart), dateTo: formatIsoDate(prevQEnd) };
    }
    case 'year': {
      return { dateFrom: formatIsoDate(new Date(y, 0, 1)), dateTo: formatIsoDate(end) };
    }
    case 'prev-year': {
      return {
        dateFrom: formatIsoDate(new Date(y - 1, 0, 1)),
        dateTo: formatIsoDate(new Date(y - 1, 11, 31)),
      };
    }
    default: {
      const _exhaustive: never = preset;
      return _exhaustive;
    }
  }
}
