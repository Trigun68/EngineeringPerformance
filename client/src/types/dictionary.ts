export interface DictionaryItem {
  id: number;
  name: string;
  code?: string | null;
  description?: string | null;
  parentId?: number | null;
}

/** 8 справочников + регистр норм ИИС (camelCase — как в API). */
export const DICTIONARY_KEYS = [
  'physicalVolumeTypes',
  'physicalVolumes',
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
  equipments: DictionaryItem[];
  laborIntensities: DictionaryItem[];
  iicWorkDirections: DictionaryItem[];
  iicWorkDirectionNorms: DictionaryItem[];
  unitsOfMeasurement: DictionaryItem[];
  projectCharacteristics: DictionaryItem[];
}
