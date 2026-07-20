import { describe, expect, it } from 'vitest';
import {
  collectForecastVolumeWarnings,
  collectMissingClassifierWarnings,
  collectNormFallbackWarnings,
} from '../normFallbackWarning';

describe('normFallbackWarning', () => {
  it('collectNormFallbackWarnings skips unallocated placeholder (workDirectionId < 0)', () => {
    const warnings = collectNormFallbackWarnings([
      {
        workDirectionId: -1,
        name: '(пусто)',
        factPhysicalVolume: 33412,
        normProductivity: null,
        baseHours: null,
      },
    ]);
    expect(warnings).toEqual([]);
  });

  it('collectNormFallbackWarnings flags IIC direction without norm/hours', () => {
    const warnings = collectNormFallbackWarnings([
      {
        workDirectionId: 10,
        name: 'IIC-1',
        factPhysicalVolume: 100,
        normProductivity: null,
        baseHours: 0,
      },
    ]);
    expect(warnings).toHaveLength(1);
    expect(warnings[0]).toContain('IIC-1');
  });

  it('collectMissingClassifierWarnings ignores Оценка=нет', () => {
    const warnings = collectMissingClassifierWarnings([
      { isAssessment: 'Нет', classifierId: null, quantity: 500 },
      { isAssessment: 'Нет', classifierId: null, quantity: 1000 },
      { isAssessment: 'Да', classifierId: null, quantity: 12.5 },
      { isAssessment: 'Да', classifierId: 5, quantity: 99 },
    ]);
    expect(warnings).toHaveLength(1);
    expect(warnings[0]).toContain('Оценка = да');
    expect(warnings[0]).toContain('12.5');
    expect(warnings[0]).not.toContain('1500');
  });

  it('collectForecastVolumeWarnings combines both sources', () => {
    const warnings = collectForecastVolumeWarnings(
      [
        {
          workDirectionId: -1,
          name: '(пусто)',
          factPhysicalVolume: 999,
          normProductivity: null,
          baseHours: null,
        },
      ],
      [{ isAssessment: 'Да', classifierId: null, quantity: 3 }],
    );
    expect(warnings).toHaveLength(1);
    expect(warnings[0]).toContain('Оценка = да');
  });
});
