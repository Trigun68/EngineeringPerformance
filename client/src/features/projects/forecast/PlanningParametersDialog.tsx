import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
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
import {
  fetchPlanningParameters,
  savePlanningParameters,
  type ProjectVersionPlanningParameter,
} from '../../../api/planningParametersApi';
import { recalculateLaborIntensities, fetchProjectVersions, updateProjectVersion, type ProjectVersion } from '../../../api/projectVersionsApi';
import { PlanningScenarioSelector } from '../../norms/PlanningScenarioSelector';
import { ZParametersCaption } from './ZParametersCaption';
import type { PlanningScenario } from '../../../types/norms';
import { getApiErrorMessage } from '../../../services/apiError';
import { usePermission } from '../../../hooks/usePermission';
import { formatIisNorm } from '../../../utils/formatIisNorm';

export interface PlanningParametersDialogProps {
  open: boolean;
  projectId: number;
  versionId: number | null;
  planningScenario?: PlanningScenario;
  onClose: () => void;
  onSaved?: () => void;
}

interface PlanningTreeRow extends ProjectVersionPlanningParameter {
  depth: number;
  hasChildren: boolean;
}

function buildPlanningTreeRows(rows: ProjectVersionPlanningParameter[]): PlanningTreeRow[] {
  const parentIds = new Set(rows.map((r) => r.parentId).filter((id): id is number => id != null));
  const byParent = rows.reduce<Map<number | null, ProjectVersionPlanningParameter[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  for (const list of byParent.values()) {
    list.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru'));
  }

  const result: PlanningTreeRow[] = [];
  const walk = (parentId: number | null, depth: number) => {
    for (const row of byParent.get(parentId) ?? []) {
      result.push({
        ...row,
        depth,
        hasChildren: parentIds.has(row.iicWorkDirectionId),
      });
      walk(row.iicWorkDirectionId, depth + 1);
    }
  };
  walk(null, 0);
  return result;
}

function formatZDisplay(value: number): string {
  return Number.isFinite(value) ? value.toFixed(2) : '0.00';
}

function parseZInput(raw: string): number | null {
  const normalized = raw.trim().replace(',', '.');
  if (normalized === '' || normalized === '-' || normalized === '.') return null;
  const value = Number(normalized);
  return Number.isFinite(value) ? value : null;
}

export function PlanningParametersDialog({
  open,
  projectId,
  versionId,
  planningScenario = 1,
  onClose,
  onSaved,
}: PlanningParametersDialogProps) {
  const { canEdit } = usePermission('Planning');
  const [rows, setRows] = useState<ProjectVersionPlanningParameter[]>([]);
  const [scenario, setScenario] = useState<PlanningScenario>(planningScenario);
  const [versionMeta, setVersionMeta] = useState<ProjectVersion | null>(null);
  const [zDrafts, setZDrafts] = useState<Record<number, { zPessimistic: string; zBase: string; zOptimistic: string }>>({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [recalculating, setRecalculating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setScenario(planningScenario);
  }, [planningScenario, open]);

  const treeRows = useMemo(() => buildPlanningTreeRows(rows), [rows]);

  const load = useCallback(async () => {
    if (!versionId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await fetchPlanningParameters(projectId, versionId);
      setRows(data);
      const versions = await fetchProjectVersions(projectId);
      const version = versions.find((v) => v.id === versionId) ?? null;
      setVersionMeta(version);
      if (version?.planningScenario) {
        setScenario(version.planningScenario as PlanningScenario);
      }
      setZDrafts(
        Object.fromEntries(
          data.map((row) => [
            row.id,
            {
              zPessimistic: formatZDisplay(row.zPessimistic),
              zBase: formatZDisplay(row.zBase),
              zOptimistic: formatZDisplay(row.zOptimistic),
            },
          ]),
        ),
      );
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить параметры планирования.'));
      setRows([]);
      setZDrafts({});
    } finally {
      setLoading(false);
    }
  }, [projectId, versionId]);

  useEffect(() => {
    if (open && versionId) {
      void load();
    }
  }, [open, versionId, load]);

  const updateRow = (id: number, field: 'zPessimistic' | 'zBase' | 'zOptimistic', raw: string) => {
    setZDrafts((prev) => ({
      ...prev,
      [id]: { ...prev[id], [field]: raw },
    }));
    const value = parseZInput(raw);
    if (value === null) return;
    setRows((prev) =>
      prev.map((row) => (row.id === id ? { ...row, [field]: value } : row)),
    );
  };

  const blurRow = (id: number, field: 'zPessimistic' | 'zBase' | 'zOptimistic') => {
    setZDrafts((prev) => {
      const draft = prev[id];
      if (!draft) return prev;
      const value = parseZInput(draft[field]);
      if (value === null) return prev;
      return {
        ...prev,
        [id]: { ...draft, [field]: formatZDisplay(value) },
      };
    });
  };

  const handleSave = async () => {
    if (!versionId || !canEdit) return;
    setSaving(true);
    setError(null);
    try {
      await savePlanningParameters(
        projectId,
        versionId,
        rows.map((row) => ({
          iicWorkDirectionId: row.iicWorkDirectionId,
          zPessimistic: row.zPessimistic,
          zBase: row.zBase,
          zOptimistic: row.zOptimistic,
        })),
      );
      if (versionMeta) {
        await updateProjectVersion(projectId, versionId, {
          versionName: versionMeta.versionName,
          description: versionMeta.description ?? null,
          direction: versionMeta.direction,
          physicalVolumeTypeId: versionMeta.physicalVolumeTypeId ?? null,
          planningScenario: scenario,
        });
      }
      setRecalculating(true);
      await recalculateLaborIntensities(projectId, versionId);
      onSaved?.();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось сохранить параметры планирования.'));
    } finally {
      setSaving(false);
      setRecalculating(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth data-testid="planning-parameters-dialog">
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        Параметры планирования
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <ZParametersCaption planVersionSelected={Boolean(versionId)} />
        <Box sx={{ mb: 2, maxWidth: 420 }}>
          <PlanningScenarioSelector value={scenario} onChange={setScenario} disabled={!canEdit || saving} />
        </Box>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={28} />
          </Box>
        ) : (
          <TableContainer component={Paper} variant="outlined">
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>Группа физ. объёмов IIC</TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>Пессимист.</TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>Базовый</TableCell>
                  <TableCell align="right" sx={{ width: 120 }}>Оптимист.</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {treeRows.map((row) => (
                  <TableRow key={row.id}>
                    <TableCell sx={{ pl: 2 + row.depth * 2.5 }}>
                      {formatIisNorm({ code: row.code, name: row.name, unitName: row.unitName })}
                    </TableCell>
                    {(['zPessimistic', 'zBase', 'zOptimistic'] as const).map((field) => (
                      <TableCell key={field} align="right">
                        <TextField
                          size="small"
                          type="number"
                          value={zDrafts[row.id]?.[field] ?? formatZDisplay(row[field])}
                          disabled={!canEdit}
                          onChange={(e) => updateRow(row.id, field, e.target.value)}
                          onBlur={() => blurRow(row.id, field)}
                          inputProps={{ style: { textAlign: 'right' }, step: '0.01' }}
                          sx={{ width: 96 }}
                        />
                      </TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Закрыть</Button>
        {canEdit && (
          <Button variant="contained" onClick={() => void handleSave()} disabled={saving || recalculating || loading || !versionId}>
            {recalculating ? 'Пересчёт…' : saving ? 'Сохранение…' : 'Сохранить'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
