import { useCallback, useEffect, useState } from 'react';
import { Checkbox, FormControlLabel, MenuItem, TextField, Typography } from '@mui/material';
import { HierarchicalSelect } from '../../components/dictionary/HierarchicalSelect';
import { fetchIicNormsByKfo } from '../../api/iicNormsApi';
import type { DirectoryFieldDef } from './directoryFieldConfig';
import { DirectoryFormRow } from './DirectoryEditDialog';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import type { DictionaryKey } from '../../types/dictionary';
import { formatIisNorm } from '../../utils/formatIisNorm';
import { resolveIicDirectionUnitName } from '../../utils/resolveIicDirectionUnit';

interface DirectoryFormFieldsProps {
  fields: DirectoryFieldDef[];
  properties: Record<string, unknown>;
  onPropertyChange: (key: string, value: unknown) => void;
  isArchived?: boolean;
  onIsArchivedChange?: (value: boolean) => void;
  showArchive?: boolean;
}

interface NormOption {
  id: number;
  label: string;
}

export function DirectoryFormFields({
  fields,
  properties,
  onPropertyChange,
  isArchived = false,
  onIsArchivedChange,
  showArchive = false,
}: DirectoryFormFieldsProps) {
  const getList = useDictionaryStore((s) => s.getList);
  const getUnitName = useDictionaryStore((s) => s.getUnitName);

  const [normOptions, setNormOptions] = useState<NormOption[]>([]);
  const [normsLoading, setNormsLoading] = useState(false);

  const kfoId = properties.kfoId == null || properties.kfoId === '' ? null : Number(properties.kfoId);

  const loadNorms = useCallback(async (kfo: number | null) => {
    if (!kfo) {
      setNormOptions([]);
      return;
    }
    setNormsLoading(true);
    try {
      const norms = await fetchIicNormsByKfo(kfo);
      setNormOptions(
        norms.map((norm) => ({
          id: norm.id,
          label: formatIisNorm({
            code: norm.workDirectionName ?? String(norm.workDirectionId),
            name: [
              norm.effectiveFrom?.slice(0, 10),
              norm.effectiveTo ? `— ${norm.effectiveTo.slice(0, 10)}` : '',
              norm.performance != null ? `П=${norm.performance}` : '',
            ]
              .filter(Boolean)
              .join(' '),
            unitName: norm.unitName ?? (norm.resolvedUnitId ? getUnitName(norm.resolvedUnitId) : undefined),
          }),
        })),
      );
    } catch {
      setNormOptions([]);
    } finally {
      setNormsLoading(false);
    }
  }, [getUnitName]);

  useEffect(() => {
    void loadNorms(kfoId);
  }, [kfoId, loadNorms]);

  const directions = getList('iicWorkDirections');

  const formatIicLabel = (item: { id: number; name: string; code?: string | null }) =>
    formatIisNorm({
      code: item.code,
      name: item.name,
      unitName: resolveIicDirectionUnitName(item.id, directions, getUnitName),
    });

  const handleKfoChange = (key: string, next: number | null) => {
    onPropertyChange(key, next);
    onPropertyChange('iicNormId', null);
  };

  return (
    <>
      {fields.map((field) => {
        const label = field.formLabel ?? field.label;

        if (field.type === 'normLookup') {
          const raw = properties[field.key];
          const value = raw == null || raw === '' ? null : Number(raw);
          const disabled = !kfoId;

          return (
            <DirectoryFormRow key={field.key} label={label}>
              <HierarchicalSelect
                items={normOptions.map((n) => ({ id: n.id, name: n.label, parentId: null }))}
                value={value}
                onChange={(next) => onPropertyChange(field.key, next)}
                disabled={disabled || normsLoading}
                emptyLabel="Не выбрано"
              />
              {disabled && (
                <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                  Сначала выберите КФО
                </Typography>
              )}
            </DirectoryFormRow>
          );
        }

        if (field.type === 'lookup' && field.lookupKey) {
          const lookupKey = field.lookupKey as DictionaryKey;
          const items = getList(lookupKey);
          const raw = properties[field.key];
          const value = raw == null || raw === '' ? null : Number(raw);

          if (field.hierarchical) {
            return (
              <DirectoryFormRow key={field.key} label={label}>
                <HierarchicalSelect
                  items={items}
                  value={value}
                  onChange={(next) =>
                    field.key === 'kfoId' ? handleKfoChange(field.key, next) : onPropertyChange(field.key, next)
                  }
                  formatLabel={field.formatAsIisNorm ? formatIicLabel : undefined}
                  required={field.required}
                />
              </DirectoryFormRow>
            );
          }

          return (
            <DirectoryFormRow key={field.key} label={label}>
              <TextField
                select
                value={value ?? ''}
                onChange={(e) =>
                  onPropertyChange(field.key, e.target.value === '' ? null : Number(e.target.value))
                }
                fullWidth
                size="small"
                required={field.required}
              >
                <MenuItem value="">(не выбрано)</MenuItem>
                {items.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {field.formatAsIisNorm ? formatIicLabel(item) : item.name}
                  </MenuItem>
                ))}
              </TextField>
            </DirectoryFormRow>
          );
        }

        if (field.type === 'assessment') {
          const raw = properties[field.key];
          const value =
            raw === true || raw === 'true' ? 'true' : raw === false || raw === 'false' ? 'false' : '';
          return (
            <DirectoryFormRow key={field.key} label={label}>
              <TextField
                select
                value={value}
                onChange={(e) => onPropertyChange(field.key, e.target.value)}
                fullWidth
                size="small"
              >
                <MenuItem value="">Не выбрано</MenuItem>
                <MenuItem value="true">Да</MenuItem>
                <MenuItem value="false">Нет</MenuItem>
              </TextField>
            </DirectoryFormRow>
          );
        }

        if (field.type === 'number') {
          const raw = properties[field.key];
          return (
            <DirectoryFormRow key={field.key} label={label}>
              <TextField
                type="number"
                value={raw == null ? '' : String(raw)}
                onChange={(e) =>
                  onPropertyChange(field.key, e.target.value === '' ? null : Number(e.target.value))
                }
                fullWidth
                size="small"
                required={field.required}
                inputProps={field.key === 'coefficient' ? { min: 0.0001, step: 0.0001 } : undefined}
              />
            </DirectoryFormRow>
          );
        }

        const raw = properties[field.key];
        return (
          <DirectoryFormRow key={field.key} label={label}>
            <TextField
              value={typeof raw === 'string' ? raw : raw == null ? '' : String(raw)}
              onChange={(e) => onPropertyChange(field.key, e.target.value)}
              fullWidth
              size="small"
              required={field.required}
            />
          </DirectoryFormRow>
        );
      })}

      {showArchive && onIsArchivedChange && (
        <DirectoryFormRow label="Архив">
          <FormControlLabel
            control={
              <Checkbox
                checked={isArchived}
                onChange={(e) => onIsArchivedChange(e.target.checked)}
              />
            }
            label="Архивная запись"
          />
        </DirectoryFormRow>
      )}
    </>
  );
}
