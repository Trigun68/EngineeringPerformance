import '@testing-library/jest-dom/vitest';

// jsdom не реализует CSS.supports — highcharts падает на импорте без заглушки.
if (typeof globalThis.CSS === 'undefined' || typeof globalThis.CSS.supports !== 'function') {
  (globalThis as Record<string, unknown>).CSS = { supports: () => false };
}
