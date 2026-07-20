import { describe, expect, it } from 'vitest';
import {
  buildPropertiesPayload,
  formatPropertyValue,
  getDirectoryFields,
  getFormFields,
  propertiesFromEntry,
} from './directoryFieldConfig';
import type { DictionaryItem } from '../../types/dictionary';

describe('physical-volumes-ber KFO link field', () => {
  it('exposes physicalVolumeKfoId in grid and form', () => {
    const fields = getDirectoryFields('physical-volumes-ber');
    const link = fields.find((f) => f.key === 'physicalVolumeKfoId');
    expect(link).toBeDefined();
    expect(link?.lookupKey).toBe('physicalVolumesKfo');
    expect(link?.hierarchical).toBe(true);
    expect(link?.showInGrid).toBe(true);
    expect(getFormFields('physical-volumes-ber').some((f) => f.key === 'physicalVolumeKfoId')).toBe(true);
  });

  it('maps physicalVolumeKfoId through payload and entry properties', () => {
    const fromEntry = propertiesFromEntry('physical-volumes-ber', { physicalVolumeKfoId: 413 });
    expect(fromEntry.physicalVolumeKfoId).toBe(413);

    const payload = buildPropertiesPayload('physical-volumes-ber', {
      ...fromEntry,
      physicalVolumeKfoId: 413,
      code: '01-01-001-01',
    });
    expect(payload.physicalVolumeKfoId).toBe(413);
  });

  it('formats linked KFO as code + name', () => {
    const field = getDirectoryFields('physical-volumes-ber').find((f) => f.key === 'physicalVolumeKfoId')!;
    const item: DictionaryItem = { id: 413, name: 'Разработка грунта', code: 'KQ.02.110', parentId: null };
    const text = formatPropertyValue(
      field,
      413,
      () => 'fallback',
      (key, id) => (key === 'physicalVolumesKfo' && id === 413 ? item : undefined),
    );
    expect(text).toBe('KQ.02.110 Разработка грунта');
  });
});
