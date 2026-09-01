/** Максимальная длина имени файла, включая расширение. */
export const EXPORT_FILE_NAME_MAX = 180;

export const ExportModules = {
  forecast: 'прогноз',
  fact: 'факт',
  compare: 'сравнение',
  report: 'отчет',
  directory: 'справочник',
  analytics: 'аналитика',
  audit: 'аудит',
  projects: 'проекты',
} as const;

export const ExportViews = {
  summary: 'итого',
  facility: 'по_объектам',
  detail: 'детально',
  analysis: 'анализ',
  version: 'версия',
  combined: 'сводный',
  catalog: 'каталог',
  auditLog: 'журнал_действий',
  norms: 'нормы',
  normHealth: 'здоровье_норм',
  factNorm: 'факт_норма',
  trends: 'динамика',
  iicGroups: 'группы_IIC',
  iicNorms: 'нормы_IIC',
  workCosts: 'стоимость_работ',
} as const;

export const ExportQualifiers = {
  visible: 'выделенные',
  summaryTable: 'сводная',
} as const;

export type BuildExportFileNameOptions = {
  module: string;
  extension?: string;
  view?: string | null;
  qualifier?: string | null;
  projectCode?: string | null;
  projectName?: string | null;
  versionLabel?: string | null;
};

const WINDOWS_INVALID = new Set('\\/:*?"<>|'.split(''));

export function sanitizeExportToken(value: string | null | undefined, keepHyphen = true): string | null {
  if (value == null || value.trim() === '') return null;

  let previousUnderscore = false;
  let result = '';
  for (const ch of value.trim()) {
    if (isAllowedChar(ch, keepHyphen)) {
      result += ch;
      previousUnderscore = false;
      continue;
    }
    if (previousUnderscore) continue;
    result += '_';
    previousUnderscore = true;
  }

  const cleaned = result.replace(/^_+|_+$/g, '');
  return cleaned === '' ? null : cleaned;
}

export function combineExportVersions(...names: Array<string | null | undefined>): string | null {
  const parts: string[] = [];
  const seen = new Set<string>();
  for (const name of names) {
    const token = sanitizeExportToken(name);
    if (!token) continue;
    const key = token.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    parts.push(token);
  }
  return parts.length === 0 ? null : parts.join('_');
}

export function directoryExportView(tabOrSlug: string | null | undefined): string {
  switch ((tabOrSlug ?? '').trim().toLowerCase()) {
    case 'physical-volumes-kfo':
      return 'ФО_KFO';
    case 'physical-volumes-ber':
      return 'ФО_BER';
    case 'equipment':
      return 'оборудование';
    case 'labor-intensities':
      return 'трудоемкость';
    case 'iic-work-directions':
      return ExportViews.iicGroups;
    case 'units-of-measurement':
      return 'единицы';
    case 'doc-sets':
      return 'комплекты_документации';
    case 'project-characteristics':
      return 'характеристики';
    case 'iic-work-direction-norms':
      return ExportViews.iicNorms;
    case 'work-costs':
    case 'iic-work-direction-work-costs':
      return ExportViews.workCosts;
    default:
      return sanitizeExportToken(tabOrSlug) ?? ExportViews.catalog;
  }
}

/**
 * Каноническое имя файла экспорта.
 * Шаблон: {модуль}_{вкладка}[_{уточнение}]_-_{код}_{название}_({версия}).{ext}
 */
export function buildCanonicalExportFileName(options: BuildExportFileNameOptions): string {
  const report = joinTokens(
    sanitizeExportToken(options.module) ?? ExportModules.report,
    sanitizeExportToken(options.view),
    sanitizeExportToken(options.qualifier),
  );
  const code = sanitizeExportToken(options.projectCode, true);
  const name = sanitizeExportToken(options.projectName);
  const version = sanitizeExportToken(options.versionLabel);
  const extension = normalizeExtension(options.extension);

  const stem = buildStem(report, code, name, version);
  return `${truncateStem(stem, report, code, name, version, extension)}.${extension}`;
}

function joinTokens(...parts: Array<string | null | undefined>): string {
  return parts.filter((part): part is string => Boolean(part)).join('_');
}

function buildStem(report: string, code: string | null, name: string | null, version: string | null): string {
  let stem = report;
  const project = joinTokens(code, name);
  if (project) stem += `_-_${project}`;
  if (version) stem += `_(${version})`;
  return stem;
}

function truncateStem(
  stem: string,
  report: string,
  code: string | null,
  name: string | null,
  version: string | null,
  extension: string,
): string {
  const maxStem = EXPORT_FILE_NAME_MAX - extension.length - 1;
  if (stem.length <= maxStem) return stem;

  if (name) {
    const reserved =
      report.length +
      (code ? 3 + code.length : 0) +
      1 +
      (version ? 3 + version.length : 0);
    const nameBudget = maxStem - reserved;
    if (nameBudget >= 1) {
      const trimmedName = name.slice(0, nameBudget).replace(/_+$/, '');
      if (trimmedName) {
        const retry = buildStem(report, code, trimmedName, version);
        if (retry.length <= maxStem) return retry;
      }
    }
    const withoutName = buildStem(report, code, null, version);
    if (withoutName.length <= maxStem) return withoutName;
  }

  return stem.slice(0, maxStem).replace(/[_(-]+$/, '');
}

function normalizeExtension(extension: string | undefined): string {
  const raw = (extension ?? 'xlsx').trim().replace(/^\.+/, '');
  return (sanitizeExportToken(raw) ?? 'xlsx').toLowerCase();
}

function isAllowedChar(ch: string, keepHyphen: boolean): boolean {
  if (ch === '(' || ch === ')') return false;
  if (/\s/.test(ch)) return false;
  if (WINDOWS_INVALID.has(ch)) return false;
  if (ch === '.') return false;
  if (ch === '-' && keepHyphen) return true;
  if (ch === '—' || ch === '_') return ch === '_';
  return true;
}
