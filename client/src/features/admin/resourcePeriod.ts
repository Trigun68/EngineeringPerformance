export interface ResourcePeriodPreset {
  minutes: number;
  label: string;
}

export const RESOURCE_PERIOD_PRESETS: ResourcePeriodPreset[] = [
  { minutes: 60, label: '1 ч' },
  { minutes: 180, label: '3 ч' },
  { minutes: 360, label: '6 ч' },
  { minutes: 720, label: '12 ч' },
  { minutes: 1440, label: '24 ч' },
  { minutes: 4320, label: '3 дн' },
  { minutes: 10080, label: '7 дн' },
  { minutes: 43200, label: '30 дн' },
  { minutes: 86400, label: '60 дн' },
  { minutes: 129600, label: '90 дн' },
];

export const DEFAULT_RESOURCE_PERIOD_MINUTES = 60;

export function resourceRefreshMs(windowMinutes: number): number {
  return windowMinutes <= 60 ? 10_000 : 60_000;
}
