import type { ProjectVersion } from '../../api/projectVersionsApi';

export const ANALYSIS_PLAN = 1;
export const ANALYSIS_FACT = 2;

export type VersionPickerDirection = typeof ANALYSIS_PLAN | typeof ANALYSIS_FACT | 'any';

export function directionLabel(direction: number): string {
  if (direction === ANALYSIS_PLAN) return 'План';
  if (direction === ANALYSIS_FACT) return 'Факт';
  return 'Прогноз';
}

export function buildVersionLabel(version: ProjectVersion): string {
  const type = directionLabel(version.direction);
  const desc = version.description?.trim();
  return desc ? `${version.versionName} — ${desc} (${type})` : `${version.versionName} (${type})`;
}

export interface VersionFormState {
  versionName: string;
  description: string;
  physicalVolumeTypeId: string;
  asOfDate: string;
  isPrimary: boolean;
}

export const emptyVersionForm = (): VersionFormState => ({
  versionName: '',
  description: '',
  physicalVolumeTypeId: '',
  asOfDate: new Date().toISOString().slice(0, 10),
  isPrimary: false,
});

export function toDateInputValue(iso?: string | null): string {
  if (!iso) return new Date().toISOString().slice(0, 10);
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return new Date().toISOString().slice(0, 10);
  return d.toISOString().slice(0, 10);
}
