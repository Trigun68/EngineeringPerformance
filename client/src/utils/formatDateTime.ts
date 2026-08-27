/**
 * Форматирует дату/время в привычный вид: ГГГГ-ММ-ДД ЧЧ:ММ:СС
 */
export function formatDateTime(isoString: string | Date | null | undefined): string {
  if (!isoString) return '';
  const date = typeof isoString === 'string' ? new Date(isoString) : isoString;
  if (isNaN(date.getTime())) return '';

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');

  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}

const DATE_ONLY_PREFIX = /^(\d{4}-\d{2}-\d{2})/;

/**
 * Форматирует только дату: ГГГГ-ММ-ДД.
 * Для ISO date-only / UTC midnight не сдвигает календарный день через локальную TZ.
 */
export function formatDate(isoString: string | Date | null | undefined): string {
  if (!isoString) return '';

  if (typeof isoString === 'string') {
    const match = DATE_ONLY_PREFIX.exec(isoString.trim());
    if (match) {
      return match[1];
    }
  }

  const date = typeof isoString === 'string' ? new Date(isoString) : isoString;
  if (isNaN(date.getTime())) return '';

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}
