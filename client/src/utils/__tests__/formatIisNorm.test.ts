import { describe, expect, it } from 'vitest';
import { formatAsIisNorm, formatIisNorm } from '../formatIisNorm';

describe('formatIisNorm', () => {
  it('formats code, name and unit', () => {
    expect(
      formatIisNorm({ code: 'IIC-1', name: 'Группа А', unitName: 'шт' }),
    ).toBe('IIC-1 Группа А (шт)');
  });

  it('omits unit when missing', () => {
    expect(formatIisNorm({ code: 'IIC-2', name: 'Группа Б' })).toBe('IIC-2 Группа Б');
  });

  it('works without code', () => {
    expect(formatIisNorm({ name: 'Без кода', unitName: 'м' })).toBe('Без кода (м)');
  });
});

describe('formatAsIisNorm', () => {
  it('delegates to formatIisNorm', () => {
    expect(formatAsIisNorm('EQ-1', 'Насос', 'шт')).toBe('EQ-1 Насос (шт)');
  });
});
