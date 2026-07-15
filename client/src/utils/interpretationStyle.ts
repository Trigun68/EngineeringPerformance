/** Цвета интерпретации — паритет ReportVisualTheme / enum PerformanceInterpretation. */
export function getInterpretationColor(text: string | null | undefined | unknown): string {
  if (text == null || text === '') return '#95A5A6';
  const lower = String(text).toLowerCase();
  if (
    lower.includes('высок') ||
    lower.includes('high') ||
    lower.includes('above') ||
    lower.includes('отлич') ||
    lower.includes('excellent') ||
    lower.includes('хорош') ||
    lower.includes('good')
  ) {
    return '#27AE60';
  }
  if (
    lower.includes('низк') ||
    lower.includes('low') ||
    lower.includes('below') ||
    lower.includes('критич') ||
    lower.includes('critical') ||
    lower.includes('вниман') ||
    lower.includes('needsattention')
  ) {
    return '#E74C3C';
  }
  if (
    lower.includes('средн') ||
    lower.includes('medium') ||
    lower.includes('moderate') ||
    lower.includes('norm') ||
    lower.includes('удовлетвор') ||
    lower.includes('satisfactory')
  ) {
    return '#F39C12';
  }
  return '#95A5A6';
}

export function interpretationCellStyle(
  text: string | null | undefined | unknown,
): { color: string; fontWeight?: number } {
  const asText = text == null || text === '' ? '' : String(text);
  return { color: getInterpretationColor(asText), fontWeight: asText ? 500 : 400 };
}
