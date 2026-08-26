import { describe, expect, it } from 'vitest';
import { formatProjectModalTitle } from '../projectModalTitle';

describe('formatProjectModalTitle', () => {
  it('joins code, name and version with em dash', () => {
    expect(
      formatProjectModalTitle('Прогноз трудозатрат', {
        code: '25-310',
        name: 'Реконструкция НС',
        version: 'Основная',
      }),
    ).toBe('Прогноз трудозатрат: 25-310 — Реконструкция НС — Основная');
  });

  it('skips empty parts and trims whitespace', () => {
    expect(
      formatProjectModalTitle('Анализ версии', {
        code: '  P-1  ',
        name: '',
        version: 'v1',
      }),
    ).toBe('Анализ версии: P-1 — v1');
  });

  it('returns prefix only when identity is empty', () => {
    expect(formatProjectModalTitle('Анализ версии', {})).toBe('Анализ версии');
    expect(
      formatProjectModalTitle('Прогноз трудозатрат', {
        code: '  ',
        name: undefined,
        version: '',
      }),
    ).toBe('Прогноз трудозатрат');
  });
});
