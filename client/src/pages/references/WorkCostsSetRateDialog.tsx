import { useCallback, useEffect, useMemo, useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import type { WorkCostBatchItem, WorkCostBatchRequest, WorkCostRow } from '../../api/registersApi';
import { saveWorkCostsBatch } from '../../api/registersApi';
import { getApiErrorMessage } from '../../services/apiError';
import { WorkingOverlay } from '../../components/WorkingOverlay/WorkingOverlay';
import {
  cascadeRate,
  filterVisibleRows,
  formatRateDisplay,
  isDirectionEditable,
  isOrganizationRowEditable,
  parseRateInput,
  WORK_COST_LEVEL,
  WORK_COST_LEVEL_LABELS,
  type WorkCostLevel,
  type WorkCostTreeNode,
} from '../../features/workCosts/workCostLevels';

export interface WorkCostsSetRateDialogProps {
  open: boolean;
  rows: WorkCostRow[];
  canEdit: boolean;
  /** Редактирование существующего периода: дата фиксируется, пустые ставки не заполняются нулями. */
  mode?: 'create' | 'edit';
  initialEffectiveFrom?: string;
  onClose: () => void;
  onSaved: () => void;
}

interface DialogTreeRow extends WorkCostTreeNode {
  depth: number;
  name: string;
}

function buildTreeRows(source: WorkCostRow[]): DialogTreeRow[] {
  const byParent = new Map<number | null, WorkCostRow[]>();
  for (const row of source) {
    const key = row.parentId ?? null;
    const list = byParent.get(key) ?? [];
    list.push(row);
    byParent.set(key, list);
  }
  for (const list of byParent.values()) {
    list.sort((a, b) => a.sortOrder - b.sortOrder || a.workDirectionName.localeCompare(b.workDirectionName, 'ru'));
  }

  const result: DialogTreeRow[] = [];
  const walk = (parentId: number | null, depth: number) => {
    for (const row of byParent.get(parentId) ?? []) {
      result.push({
        id: row.workDirectionId,
        parentId: row.parentId ?? null,
        ratePerHour: row.ratePerHour ?? null,
        depth,
        name: row.workDirectionName,
      });
      walk(row.workDirectionId, depth + 1);
    }
  };
  walk(null, 0);
  return result;
}

function uniformRate(rows: WorkCostRow[]): number | null {
  const rates = rows
    .map((r) => r.ratePerHour)
    .filter((v): v is number => v != null && Number.isFinite(v));
  if (rates.length === 0) return null;
  const first = rates[0];
  return rates.every((r) => r === first) ? first : null;
}

export function WorkCostsSetRateDialog({
  open,
  rows: sourceRows,
  canEdit,
  mode = 'create',
  initialEffectiveFrom,
  onClose,
  onSaved,
}: WorkCostsSetRateDialogProps) {
  const [level, setLevel] = useState<WorkCostLevel>(WORK_COST_LEVEL.EachElement);
  const [effectiveFrom, setEffectiveFrom] = useState(() => new Date().toISOString().slice(0, 10));
  const [tree, setTree] = useState<DialogTreeRow[]>([]);
  const [drafts, setDrafts] = useState<Record<number, string>>({});
  const [orgDraft, setOrgDraft] = useState('0,00');
  const [orgRate, setOrgRate] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const { mutate: saveRates, isPending: saving } = useMutation({
    mutationFn: (body: WorkCostBatchRequest) => saveWorkCostsBatch(body),
    onSuccess: () => {
      onSaved();
      onClose();
    },
    onError: (err) => {
      setError(getApiErrorMessage(err, 'Не удалось сохранить стоимость работ.'));
    },
  });

  useEffect(() => {
    if (!open) return;
    const built = buildTreeRows(sourceRows);
    setTree(built);
    setLevel(WORK_COST_LEVEL.EachElement);
    setEffectiveFrom(
      initialEffectiveFrom?.slice(0, 10)
        ?? new Date().toISOString().slice(0, 10),
    );
    setDrafts(
      Object.fromEntries(
        built.map((r) => [
          r.id,
          mode === 'edit' && r.ratePerHour == null ? '' : formatRateDisplay(r.ratePerHour ?? 0),
        ]),
      ),
    );
    const shared = uniformRate(sourceRows);
    setOrgDraft(shared == null ? '0,00' : formatRateDisplay(shared));
    setOrgRate(shared ?? 0);
    setError(null);
  }, [initialEffectiveFrom, mode, open, sourceRows]);

  const visibleRows = useMemo(() => filterVisibleRows(tree, level), [tree, level]);
  const orgEditable = canEdit && isOrganizationRowEditable(level);

  const applyCascade = (source: 'organization' | number, rate: number) => {
    setTree((prev) => {
      const nextNodes = cascadeRate(prev, source, rate);
      const next = prev.map((row, index) => ({ ...row, ratePerHour: nextNodes[index]?.ratePerHour ?? row.ratePerHour }));
      setDrafts((current) => {
        const formatted = Object.fromEntries(
          next.map((r) => [r.id, formatRateDisplay(r.ratePerHour ?? 0)]),
        );
        if (source === 'organization') {
          return { ...formatted };
        }
        if (current[source] !== undefined) formatted[source] = current[source];
        return formatted;
      });
      return next;
    });
  };

  const updateDirection = (id: number, raw: string) => {
    setDrafts((prev) => ({ ...prev, [id]: raw }));
    const value = parseRateInput(raw);
    if (value === null) return;
    if (level === WORK_COST_LEVEL.EachElement) {
      setTree((prev) => prev.map((row) => (row.id === id ? { ...row, ratePerHour: value } : row)));
      return;
    }
    applyCascade(id, value);
  };

  const updateOrganization = (raw: string) => {
    setOrgDraft(raw);
    const value = parseRateInput(raw);
    if (value === null) return;
    setOrgRate(value);
    applyCascade('organization', value);
  };

  const blurDirection = (id: number) => {
    setDrafts((prev) => {
      const parsed = parseRateInput(prev[id] ?? '');
      if (parsed === null) return prev;
      return { ...prev, [id]: formatRateDisplay(parsed) };
    });
  };

  const blurOrganization = () => {
    const parsed = parseRateInput(orgDraft);
    if (parsed === null) return;
    setOrgDraft(formatRateDisplay(parsed));
  };

  const handleSave = useCallback(() => {
    if (!canEdit) return;
    if (!effectiveFrom) {
      setError('Укажите дату «Действует с».');
      return;
    }

    let items: WorkCostBatchItem[];
    if (isOrganizationRowEditable(level)) {
      const rate = parseRateInput(orgDraft) ?? orgRate;
      items = [{ workDirectionId: 0, ratePerHour: rate }];
    } else {
      // На сервер уходят только видимые узлы выбранного уровня; каскад в потомков делает API.
      items = visibleRows.flatMap((row) => {
        const parsed = parseRateInput(drafts[row.id] ?? '');
        const rate = parsed ?? row.ratePerHour;
        if (rate == null || !Number.isFinite(rate)) return [];
        return [{ workDirectionId: row.id, ratePerHour: rate }];
      });
    }

    if (items.length === 0) {
      setError('Не задана ни одна ставка.');
      return;
    }

    setError(null);
    saveRates({ effectiveFrom, level, items });
  }, [canEdit, drafts, effectiveFrom, level, orgDraft, orgRate, saveRates, visibleRows]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth data-testid="work-costs-set-rate-dialog">
      <WorkingOverlay open={saving} delayMs={0} />
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        {mode === 'edit' ? 'Изменить стоимость' : 'Задать стоимость'}
        <IconButton aria-label="Закрыть диалог" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Box sx={{ mb: 2, display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 420 }}>
          <TextField
            label="Действует с"
            type="date"
            size="small"
            required
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            InputLabelProps={{ shrink: true }}
            disabled={!canEdit || saving || mode === 'edit'}
          />
          <TextField
            select
            size="small"
            fullWidth
            label="Уровень"
            value={level}
            disabled={!canEdit || saving}
            onChange={(e) => setLevel(Number(e.target.value) as WorkCostLevel)}
            data-testid="work-cost-level-selector"
          >
            {(Object.entries(WORK_COST_LEVEL_LABELS) as Array<[string, string]>).map(([k, label]) => (
              <MenuItem key={k} value={Number(k)}>
                {label}
              </MenuItem>
            ))}
          </TextField>
        </Box>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <TableContainer component={Paper} variant="outlined">
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell>Группа</TableCell>
                <TableCell align="right" sx={{ width: 140 }}>
                  Стоимость, ₽/ч
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              <TableRow data-testid="work-cost-org-row">
                <TableCell sx={{ fontWeight: 600 }}>В целом по организации</TableCell>
                <TableCell align="right">
                  <TextField
                    size="small"
                    type="text"
                    inputMode="decimal"
                    value={orgDraft}
                    disabled={!orgEditable}
                    onChange={(e) => updateOrganization(e.target.value)}
                    onBlur={blurOrganization}
                    inputProps={{
                      style: { textAlign: 'right' },
                      'aria-label': 'Стоимость в целом по организации',
                    }}
                    sx={{ width: 112 }}
                  />
                </TableCell>
              </TableRow>
              {visibleRows.map((row) => {
                const editable = canEdit && isDirectionEditable(level, row.depth);
                return (
                  <TableRow key={row.id}>
                    <TableCell sx={{ pl: 2 + row.depth * 2.5 }}>{row.name}</TableCell>
                    <TableCell align="right">
                      <TextField
                        size="small"
                        type="text"
                        inputMode="decimal"
                        value={drafts[row.id] ?? formatRateDisplay(row.ratePerHour ?? 0)}
                        disabled={!editable}
                        onChange={(e) => updateDirection(row.id, e.target.value)}
                        onBlur={() => blurDirection(row.id)}
                        inputProps={{
                          style: { textAlign: 'right' },
                          'aria-label': `Стоимость ${row.name}`,
                        }}
                        sx={{ width: 112 }}
                      />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Закрыть</Button>
        {canEdit && (
          <Button variant="contained" onClick={() => void handleSave()} disabled={saving}>
            {saving ? 'Сохранение…' : 'Сохранить'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
