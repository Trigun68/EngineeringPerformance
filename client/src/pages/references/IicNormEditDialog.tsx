import { Box, Button, Stack, TextField, Typography } from '@mui/material';
import type { IicNormHistoryItem, IicNormWriteRequest } from '../../api/registersApi';
import { HierarchicalSelect } from '../../components/dictionary/HierarchicalSelect';
import { DeviationFormulaEditor } from '../../features/norms/DeviationFormulaEditor';
import { NormFormulaEditor } from '../../features/norms/NormFormulaEditor';
import { applyNormFormulaFields, formulaTypeLabel, isKimCurveType } from '../../features/norms/kimCurve';
import { defaultDeviationFormula, defaultNormFormula } from '../../types/norms';
import type { DictionaryItem } from '../../types/dictionary';
import { formatIisNorm } from '../../utils/formatIisNorm';
import { formatDate } from '../../utils/formatDateTime';
import { resolveIicDirectionUnitId } from '../../utils/resolveIicDirectionUnit';
import { DirectoryEditDialog, DirectoryFormRow } from './DirectoryEditDialog';

export interface IicNormEditDialogProps {
  open: boolean;
  editing: boolean;
  canEdit: boolean;
  form: IicNormWriteRequest;
  onFormChange: (next: IicNormWriteRequest) => void;
  directions: DictionaryItem[];
  getUnitLabel: (directionId: number) => string;
  directionLabel: string;
  resolvedUnitLabel: string;
  historyRows: IicNormHistoryItem[];
  selectedHistory: IicNormHistoryItem | null;
  onSelectHistory: (row: IicNormHistoryItem) => void;
  onAddHistory: () => void;
  onEditHistory: (row?: IicNormHistoryItem | null) => void;
  onDeleteHistory: () => void;
  onClose: () => void;
  onSubmit: () => Promise<void> | void;
}

export function IicNormEditDialog({
  open,
  editing,
  canEdit,
  form,
  onFormChange,
  directions,
  getUnitLabel,
  directionLabel,
  resolvedUnitLabel,
  historyRows,
  selectedHistory,
  onSelectHistory,
  onAddHistory,
  onEditHistory,
  onDeleteHistory,
  onClose,
  onSubmit,
}: IicNormEditDialogProps) {
  return (
    <DirectoryEditDialog
      open={open}
      title={editing ? 'Редактирование нормы' : 'Новая норма'}
      onClose={onClose}
      onSubmit={onSubmit}
      maxWidth="md"
    >
      <DirectoryFormRow label="Группа">
        {editing ? (
          <TextField value={directionLabel} fullWidth size="small" InputProps={{ readOnly: true }} />
        ) : (
          <HierarchicalSelect
            items={directions}
            value={form.workDirectionId || null}
            onChange={(value) =>
              onFormChange({
                ...form,
                workDirectionId: value ?? 0,
                unitId: value ? resolveIicDirectionUnitId(value, directions) : null,
              })
            }
            formatLabel={(item) =>
              formatIisNorm({ code: item.code, name: item.name, unitName: getUnitLabel(item.id) })
            }
            allowEmpty={false}
            required
          />
        )}
      </DirectoryFormRow>
      <DirectoryFormRow label="Ед. изм.">
        <TextField
          value={resolvedUnitLabel || '—'}
          fullWidth
          size="small"
          InputProps={{ readOnly: true }}
        />
      </DirectoryFormRow>

      {!editing && (
        <>
          <DirectoryFormRow label="Параметрическая норма N(V)">
            <NormFormulaEditor
              value={form.normFormula ?? defaultNormFormula(form.performance ?? 0)}
              deviationFormula={form.deviationFormula ?? defaultDeviationFormula()}
              onChange={(normFormula) => {
                const next = applyNormFormulaFields(
                  normFormula,
                  form.deviationFormula ?? defaultDeviationFormula(),
                );
                onFormChange({
                  ...form,
                  normFormula: next.normFormula,
                  deviationFormula: next.deviationFormula,
                  performance: next.performance,
                  estimate: isKimCurveType(next.normFormula.type) ? next.estimate : form.estimate,
                  standardDeviation: isKimCurveType(next.normFormula.type)
                    ? next.standardDeviation
                    : form.standardDeviation,
                });
              }}
            />
          </DirectoryFormRow>
          {!isKimCurveType(form.normFormula?.type) && (
            <DirectoryFormRow label="Отклонение от нормы, %">
              <DeviationFormulaEditor
                value={form.deviationFormula ?? defaultDeviationFormula()}
                onChange={(deviationFormula) => {
                  const sigma = deviationFormula.coefficients.sigma ?? 0;
                  const perf = form.normFormula?.coefficients.a ?? form.performance ?? 0;
                  onFormChange({
                    ...form,
                    deviationFormula,
                    standardDeviation: perf > 0 ? sigma * perf : form.standardDeviation,
                  });
                }}
              />
            </DirectoryFormRow>
          )}
          <DirectoryFormRow label="Действует с">
            <TextField
              type="date"
              value={form.effectiveFrom}
              onChange={(e) => onFormChange({ ...form, effectiveFrom: e.target.value })}
              fullWidth
              size="small"
              InputLabelProps={{ shrink: true }}
              required
            />
          </DirectoryFormRow>
          <DirectoryFormRow label="Действует по">
            <TextField
              type="date"
              value={form.effectiveTo ?? ''}
              onChange={(e) => onFormChange({ ...form, effectiveTo: e.target.value || null })}
              fullWidth
              size="small"
              InputLabelProps={{ shrink: true }}
            />
          </DirectoryFormRow>
        </>
      )}

      {editing && form.workDirectionId > 0 && (
        <DirectoryFormRow label="Исторические записи">
          <Box sx={{ width: '100%' }}>
            <Stack direction="row" spacing={1} sx={{ mb: 1 }} flexWrap="wrap">
              {canEdit && (
                <>
                  <Button size="small" variant="outlined" onClick={onAddHistory}>
                    Добавить строку
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={() => onEditHistory()}
                    disabled={!selectedHistory}
                  >
                    Редактировать
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    color="error"
                    onClick={onDeleteHistory}
                    disabled={!selectedHistory}
                  >
                    Удалить
                  </Button>
                </>
              )}
            </Stack>

            <Stack
              direction="row"
              spacing={1}
              sx={{ mb: 0.5, px: 1, py: 0.5, backgroundColor: 'grey.100', borderRadius: 1, fontWeight: 600, fontSize: '0.75rem', color: 'text.secondary' }}
            >
              <Box sx={{ width: 140 }}>Действует с</Box>
              <Box sx={{ width: 140 }}>Действует по</Box>
              <Box sx={{ width: 110 }}>Производительность</Box>
              <Box sx={{ width: 90 }}>Тип формулы</Box>
              <Box sx={{ width: 80 }}>μ</Box>
              <Box sx={{ width: 110 }}>σ</Box>
            </Stack>

            {historyRows.map((row) => {
              const isSelected = selectedHistory?.id === row.id;
              return (
                <Stack
                  key={row.id ?? row.effectiveFrom}
                  direction="row"
                  spacing={1}
                  data-testid={row.id != null ? `iic-norm-history-row-${row.id}` : 'iic-norm-history-row-new'}
                  onClick={() => onSelectHistory(row)}
                  onDoubleClick={() => {
                    if (canEdit) onEditHistory(row);
                  }}
                  sx={{
                    mb: 0.5,
                    px: 1,
                    py: 0.75,
                    cursor: canEdit ? 'pointer' : 'default',
                    borderRadius: 1,
                    bgcolor: isSelected ? 'action.selected' : undefined,
                    '&:hover': { bgcolor: isSelected ? 'action.selected' : 'action.hover' },
                  }}
                >
                  <Typography variant="body2" sx={{ width: 140 }}>
                    {formatDate(row.effectiveFrom)}
                  </Typography>
                  <Typography variant="body2" sx={{ width: 140 }}>
                    {row.effectiveTo ? formatDate(row.effectiveTo) : '—'}
                  </Typography>
                  <Typography variant="body2" sx={{ width: 110 }}>
                    {row.performance ?? '—'}
                  </Typography>
                  <Typography variant="body2" sx={{ width: 90 }}>
                    {formulaTypeLabel(row.normFormula?.type)}
                  </Typography>
                  <Typography variant="body2" sx={{ width: 80 }}>
                    {row.estimate ?? '—'}
                  </Typography>
                  <Typography variant="body2" sx={{ width: 110 }}>
                    {row.standardDeviation ?? '—'}
                  </Typography>
                </Stack>
              );
            })}
          </Box>
        </DirectoryFormRow>
      )}
    </DirectoryEditDialog>
  );
}
