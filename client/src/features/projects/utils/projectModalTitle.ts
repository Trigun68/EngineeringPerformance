/** Размер шапки колонок модалок прогноза/анализа. */
export const REPORT_COLUMN_HEADER_FONT_SIZE = '1rem';

export interface ProjectModalTitleParts {
  code?: string;
  name?: string;
  version?: string;
}

/** «Прогноз трудозатрат: 25-310 — Реконструкция НС — Основная»; пустые части пропускаются. */
export function formatProjectModalTitle(
  prefix: string,
  parts: ProjectModalTitleParts,
): string {
  const identity = [parts.code, parts.name, parts.version]
    .map((part) => part?.trim() ?? '')
    .filter((part) => part.length > 0);
  return identity.length > 0 ? `${prefix}: ${identity.join(' — ')}` : prefix;
}
