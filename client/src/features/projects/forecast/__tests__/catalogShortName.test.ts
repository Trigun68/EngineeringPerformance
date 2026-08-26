import { describe, expect, it } from 'vitest';
import { shortCatalogName } from '../catalogShortName';

describe('shortCatalogName', () => {
  it('takes text before first colon', () => {
    expect(shortCatalogName('КФО: Классификатор физических объёмов')).toBe('КФО');
    expect(shortCatalogName('БЭР: Базовая единица')).toBe('БЭР');
  });

  it('returns full name when no colon', () => {
    expect(shortCatalogName('КФО')).toBe('КФО');
    expect(shortCatalogName('БЭР')).toBe('БЭР');
  });

  it('trims and handles empty', () => {
    expect(shortCatalogName('  КФО  ')).toBe('КФО');
    expect(shortCatalogName('')).toBeNull();
    expect(shortCatalogName(null)).toBeNull();
    expect(shortCatalogName(undefined)).toBeNull();
    expect(shortCatalogName(' : rest')).toBeNull();
  });
});
