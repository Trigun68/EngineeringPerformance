import { useEffect, useState } from 'react';
import { CircularProgress, Box } from '@mui/material';
import { fetchIicNormById } from '../../../api/registersApi';
import { IicNormHistoryDialog } from '../../../pages/references/IicNormHistoryDialog';
import type { IicNormHistoryItem } from '../../../api/registersApi';

export interface ForecastDetailNormDialogProps {
  open: boolean;
  normId: number | null;
  groupLabel: string;
  unitLabel: string;
  workDirectionId: number | null;
  onClose: () => void;
  onSaved: () => void;
}

function toHistoryItem(norm: Awaited<ReturnType<typeof fetchIicNormById>>): IicNormHistoryItem {
  return {
    id: norm.id,
    effectiveFrom: norm.effectiveFrom,
    effectiveTo: norm.effectiveTo ?? null,
    performance: norm.performance ?? null,
    estimate: norm.estimate ?? null,
    standardDeviation: norm.standardDeviation ?? null,
    unitId: norm.resolvedUnitId ?? norm.unitId ?? null,
    normFormula: norm.normFormula ?? null,
    deviationFormula: norm.deviationFormula ?? null,
    migratedAt: norm.migratedAt ?? null,
  };
}

export function ForecastDetailNormDialog({
  open,
  normId,
  groupLabel,
  unitLabel,
  workDirectionId,
  onClose,
  onSaved,
}: ForecastDetailNormDialogProps) {
  const [initial, setInitial] = useState<IicNormHistoryItem | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open || !normId) {
      setInitial(null);
      return;
    }
    setLoading(true);
    void fetchIicNormById(normId)
      .then((norm) => setInitial(toHistoryItem(norm)))
      .catch(() => setInitial(null))
      .finally(() => setLoading(false));
  }, [normId, open]);

  if (!open || !workDirectionId) return null;

  if (loading || !initial) {
    return (
      <Box sx={{ display: open ? 'flex' : 'none', justifyContent: 'center', py: 4 }}>
        {open && <CircularProgress size={28} />}
      </Box>
    );
  }

  return (
    <IicNormHistoryDialog
      open={open}
      title="Редактировать норму IIC"
      groupLabel={groupLabel}
      unitLabel={unitLabel}
      workDirectionId={workDirectionId}
      initial={initial}
      onClose={onClose}
      onSaved={() => onSaved()}
    />
  );
}
