import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

export function fmt(value: number | null | undefined, digits = 2): string {
  if (value == null || Number.isNaN(value)) return '—';
  return value.toLocaleString('ru-RU', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });
}

export function fmtSigned(value: number | null | undefined, digits = 2): string {
  if (value == null || Number.isNaN(value)) return '—';
  const formatted = fmt(Math.abs(value), digits);
  if (value > 0) return `+${formatted}`;
  if (value < 0) return `−${formatted}`;
  return formatted;
}

export function interpretationColor(z: number | null | undefined): string {
  if (z == null || Number.isNaN(z)) return 'var(--ep-report-muted-foreground)';
  if (z < -1) return 'var(--ep-report-perf-excellent)';
  if (z < -0.5) return 'var(--ep-report-perf-good)';
  if (z <= 0.5) return 'var(--ep-report-perf-satisfactory)';
  if (z <= 1) return 'var(--ep-report-perf-attention)';
  return 'var(--ep-report-perf-critical)';
}
