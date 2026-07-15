import { describe, expect, it } from 'vitest';
import { buildExportFileName } from '../directoryExport';

describe('buildExportFileName', () => {
  it('sanitizes invalid characters in tab name', () => {
    const name = buildExportFileName('KFO/BER');
    expect(name).toMatch(/^KFO_BER_\d{8}_\d{6}\.xlsx$/);
  });

  it('uses fallback when tab name is empty after sanitize', () => {
    const name = buildExportFileName('???');
    expect(name).toMatch(/^Справочник_\d{8}_\d{6}\.xlsx$/);
  });
});
