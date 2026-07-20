export interface DictionaryItem {
  id: number;
  name: string;
  code?: string | null;
  description?: string | null;
  parentId?: number | null;
  unitId?: number | null;
}

/** Справочники + регистр норм ИИС (camelCase — как в API). */
export const DICTIONARY_KEYS = [
  'physicalVolumeTypes',
  'physicalVolumes',
  'physicalVolumesKfo',
  'physicalVolumesBer',
  'equipments',
  'laborIntensities',
  'iicWorkDirections',
  'iicWorkDirectionNorms',
  'unitsOfMeasurement',
  'projectCharacteristics',
] as const;

export type DictionaryKey = (typeof DICTIONARY_KEYS)[number];

export interface DictionariesAllResponse {
  physicalVolumeTypes: DictionaryItem[];
  physicalVolumes: DictionaryItem[];
  physicalVolumesKfo: DictionaryItem[];
  physicalVolumesBer: DictionaryItem[];
  equipments: DictionaryItem[];
  laborIntensities: DictionaryItem[];
  iicWorkDirections: DictionaryItem[];
  iicWorkDirectionNorms: DictionaryItem[];
  unitsOfMeasurement: DictionaryItem[];
  projectCharacteristics: DictionaryItem[];
}
