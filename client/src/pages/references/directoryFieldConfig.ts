import type { DictionaryKey, DictionaryItem } from '../../types/dictionary';
import { formatIisNorm } from '../../utils/formatIisNorm';

export type DirectoryFieldType = 'text' | 'number' | 'lookup' | 'assessment' | 'normLookup';

export interface DirectoryFieldDef {
  key: string;
  label: string;
  formLabel?: string;
  type: DirectoryFieldType;
  lookupKey?: DictionaryKey;
  hierarchical?: boolean;
  formatAsIisNorm?: boolean;
  /** Ключ свойства в Entity (если отличается от key, напр. kfoId → classifierId). */
  propertyKey?: string;
  /** Для normLookup: поле-зависимость (kfoId). */
  dependsOn?: string;
  required?: boolean;
  showInGrid?: boolean;
  showInForm?: boolean;
  readOnly?: boolean;
  gridWidth?: number;
  formOrder?: number;
}

export const DIRECTORY_FIELD_CONFIG: Record<string, DirectoryFieldDef[]> = {
  'units-of-measurement': [
    { key: 'description', label: 'Описание', type: 'text', showInGrid: true, gridWidth: 240, formOrder: 20 },
  ],
  'doc-sets': [
    { key: 'code', label: 'Код', type: 'text', required: true, showInGrid: true, gridWidth: 140, formOrder: 10 },
  ],
  'physical-volumes-kfo': [
    { key: 'code', label: 'Код', type: 'text', required: true, showInGrid: false, gridWidth: 120, formOrder: 10 },
    { key: 'description', label: 'Описание', type: 'text', showInGrid: true, gridWidth: 200, formOrder: 30 },
    {
      key: 'kfoId',
      label: 'Классификатор (КФО)',
      formLabel: 'Классификатор (КФО/нормы IIC)',
      type: 'lookup',
      lookupKey: 'iicWorkDirections',
      propertyKey: 'classifierId',
      hierarchical: true,
      formatAsIisNorm: true,
      showInGrid: true,
      gridWidth: 200,
      formOrder: 50,
    },
    { key: 'coefficient', label: 'Коэффициент', type: 'number', showInGrid: true, gridWidth: 110, formOrder: 70 },
    { key: 'unitId', label: 'Ед. изм.', type: 'lookup', lookupKey: 'unitsOfMeasurement', showInGrid: true, gridWidth: 100, formOrder: 80 },
    { key: 'isAssessment', label: 'Оценка', type: 'assessment', showInGrid: true, gridWidth: 90, formOrder: 90 },
  ],
  'physical-volumes-ber': [
    { key: 'code', label: 'Код', type: 'text', required: true, showInGrid: false, gridWidth: 120, formOrder: 10 },
    { key: 'description', label: 'Описание', type: 'text', showInGrid: true, gridWidth: 200, formOrder: 30 },
    {
      key: 'physicalVolumeKfoId',
      label: 'Связанная норма КФО',
      formLabel: 'Связанная номенклатура КФО',
      type: 'lookup',
      lookupKey: 'physicalVolumesKfo',
      hierarchical: true,
      showInGrid: true,
      gridWidth: 260,
      formOrder: 40,
    },
    {
      key: 'kfoId',
      label: 'Классификатор (IIC)',
      formLabel: 'Классификатор (группы физ. объёмов / нормы IIC)',
      type: 'lookup',
      lookupKey: 'iicWorkDirections',
      propertyKey: 'classifierId',
      hierarchical: true,
      formatAsIisNorm: true,
      showInGrid: true,
      gridWidth: 200,
      formOrder: 50,
    },
    { key: 'coefficient', label: 'Коэффициент', type: 'number', showInGrid: true, gridWidth: 110, formOrder: 70 },
    { key: 'unitId', label: 'Ед. изм.', type: 'lookup', lookupKey: 'unitsOfMeasurement', showInGrid: true, gridWidth: 100, formOrder: 80 },
    { key: 'isAssessment', label: 'Оценка', type: 'assessment', showInGrid: true, gridWidth: 90, formOrder: 90 },
  ],
  'iic-work-directions': [
    { key: 'code', label: 'Код', type: 'text', showInGrid: true, gridWidth: 100, formOrder: 10 },
    { key: 'description', label: 'Описание', type: 'text', showInGrid: true, gridWidth: 200, formOrder: 30 },
    {
      key: 'unitId',
      label: 'Ед. измерения',
      type: 'lookup',
      lookupKey: 'unitsOfMeasurement',
      showInGrid: true,
      gridWidth: 120,
      formOrder: 40,
    },
  ],
  'labor-intensities': [
    { key: 'code', label: 'Код', type: 'text', showInGrid: true, gridWidth: 120, formOrder: 10 },
    {
      key: 'documentTypeName',
      label: 'Тип документа TF',
      type: 'text',
      showInGrid: true,
      showInForm: false,
      readOnly: true,
      gridWidth: 280,
    },
    {
      key: 'iicWorkDirectionId',
      label: 'Направление IIC',
      formLabel: 'Группа IIC',
      type: 'lookup',
      lookupKey: 'iicWorkDirections',
      hierarchical: true,
      formatAsIisNorm: true,
      showInGrid: true,
      showInForm: false,
      readOnly: true,
      gridWidth: 280,
      formOrder: 40,
    },
    { key: 'normPpr', label: 'Норма ПИР', type: 'number', showInGrid: false, showInForm: false, readOnly: true, gridWidth: 100 },
    { key: 'normOpr', label: 'Норма ОПР', type: 'number', showInGrid: false, showInForm: false, readOnly: true, gridWidth: 100 },
    { key: 'normPd', label: 'Норма ПД', type: 'number', showInGrid: false, showInForm: false, readOnly: true, gridWidth: 100 },
    { key: 'normRd', label: 'Норма РД', type: 'number', showInGrid: false, showInForm: false, readOnly: true, gridWidth: 100 },
  ],
  equipment: [
    { key: 'description', label: 'Описание', type: 'text', showInGrid: true, gridWidth: 200, formOrder: 20 },
    {
      key: 'classifierId',
      label: 'Классификатор',
      type: 'lookup',
      lookupKey: 'iicWorkDirections',
      hierarchical: true,
      formatAsIisNorm: true,
      showInGrid: true,
      gridWidth: 200,
      formOrder: 30,
    },
    { key: 'unitId', label: 'Ед. изм.', type: 'lookup', lookupKey: 'unitsOfMeasurement', showInGrid: true, gridWidth: 100, formOrder: 50 },
    { key: 'coefficient', label: 'Коэффициент', type: 'number', showInGrid: true, gridWidth: 110, formOrder: 60 },
    { key: 'isAssessment', label: 'Оценка', type: 'assessment', showInGrid: true, gridWidth: 90, formOrder: 70 },
  ],
  'project-characteristics': [],
};

export function getDirectoryFields(resource: string): DirectoryFieldDef[] {
  return DIRECTORY_FIELD_CONFIG[resource] ?? [];
}

function entityPropertyKey(field: DirectoryFieldDef): string {
  return field.propertyKey ?? field.key;
}

export function getFormFields(resource: string): DirectoryFieldDef[] {
  return getDirectoryFields(resource)
    .filter((f) => f.showInForm !== false && !f.readOnly)
    .sort((a, b) => (a.formOrder ?? 99) - (b.formOrder ?? 99));
}

export function emptyProperties(resource: string): Record<string, unknown> {
  const props: Record<string, unknown> = {};
  for (const field of getDirectoryFields(resource)) {
    if (field.type === 'number') {
      props[field.key] = field.key === 'coefficient' ? 1 : null;
    } else if (field.type === 'assessment') {
      props[field.key] = null;
    } else if (field.type === 'lookup') {
      props[field.key] = null;
    } else {
      props[field.key] = '';
    }
  }
  return props;
}

export function propertiesFromEntry(
  resource: string,
  properties: Record<string, unknown>,
): Record<string, unknown> {
  const result = emptyProperties(resource);
  for (const field of getDirectoryFields(resource)) {
    const entityKey = entityPropertyKey(field);
    if (entityKey in properties) {
      result[field.key] = properties[entityKey];
    } else if (field.key in properties) {
      result[field.key] = properties[field.key];
    }
  }
  return result;
}

export function buildPropertiesPayload(
  resource: string,
  properties: Record<string, unknown>,
): Record<string, unknown> {
  const payload: Record<string, unknown> = {};
  for (const field of getDirectoryFields(resource)) {
    if (field.readOnly && field.showInForm === false) continue;
    const value = properties[field.key];
    const targetKey = entityPropertyKey(field);
    if (field.type === 'text') {
      payload[targetKey] = typeof value === 'string' ? value : String(value ?? '');
    } else if (field.type === 'number' || field.type === 'normLookup') {
      payload[targetKey] = value === '' || value == null ? (field.key === 'coefficient' ? 1 : null) : Number(value);
    } else if (field.type === 'lookup') {
      payload[targetKey] = value === '' || value == null ? null : Number(value);
    } else if (field.type === 'assessment') {
      if (value === 'true' || value === true) payload[targetKey] = true;
      else if (value === 'false' || value === false) payload[targetKey] = false;
      else payload[targetKey] = null;
    }
  }
  return payload;
}

export function formatPropertyValue(
  field: DirectoryFieldDef,
  value: unknown,
  getName: (key: DictionaryKey, id: number | null | undefined) => string,
  getItem?: (key: DictionaryKey, id: number) => DictionaryItem | undefined,
  getUnitName?: (id: number | null | undefined) => string,
  resolveIicUnitName?: (directionId: number) => string,
): string {
  if (value == null || value === '') return '';
  if (field.type === 'lookup' && field.lookupKey) {
    const id = Number(value);
    if (field.formatAsIisNorm) {
      const item = getItem?.(field.lookupKey, id);
      if (item) {
        const unitName = field.lookupKey === 'iicWorkDirections' && resolveIicUnitName
          ? resolveIicUnitName(id)
          : getUnitName?.(item.unitId ?? null);
        return formatIisNorm({
          code: item.code,
          name: item.name,
          unitName,
        });
      }
    }
    if (field.lookupKey === 'physicalVolumesKfo' || field.lookupKey === 'physicalVolumesBer') {
      const item = getItem?.(field.lookupKey, id);
      if (item) {
        return item.code?.trim() ? `${item.code} ${item.name}` : item.name;
      }
    }
    return getName(field.lookupKey, id);
  }
  if (field.type === 'normLookup') {
    return value == null ? '' : String(value);
  }
  if (field.type === 'assessment') {
    if (value === true) return 'Да';
    if (value === false) return 'Нет';
    return '';
  }
  if (field.type === 'number') {
    return String(value);
  }
  return String(value);
}
