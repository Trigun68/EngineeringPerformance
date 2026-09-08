import { useCallback } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createIicNorm,
  deleteIicNorm,
  type IicNorm,
  type IicNormHistoryItem,
  type IicNormWriteRequest,
} from '../../api/registersApi';
import { useConfirmation } from '../../hooks/useConfirmation';
import { queryKeys } from '../../services/queryKeys';
import { clearDictionariesHttpCache } from '../../store/useDictionaryStore';
import { applyNormFormulaFields, isKimCurveType } from '../../features/norms/kimCurve';
import { defaultDeviationFormula, defaultNormFormula } from '../../types/norms';
import { resolveIicDirectionUnitId } from '../../utils/resolveIicDirectionUnit';
import type { DictionaryItem } from '../../types/dictionary';
import { normalizeDeviationFormula, normalizeNormFormula } from '../../utils/normalizeNormFormulas';

export function emptyIicNormForm(): IicNormWriteRequest {
  return {
    workDirectionId: 0,
    performance: null,
    estimate: null,
    standardDeviation: null,
    unitId: null,
    effectiveFrom: new Date().toISOString().slice(0, 10),
    effectiveTo: null,
    normFormula: defaultNormFormula(),
    deviationFormula: defaultDeviationFormula(),
  };
}

export function formFromNorm(row: IicNorm): IicNormWriteRequest {
  return {
    workDirectionId: row.workDirectionId,
    performance: row.performance ?? null,
    estimate: row.estimate ?? null,
    standardDeviation: row.standardDeviation ?? null,
    unitId: row.resolvedUnitId ?? row.unitId ?? null,
    effectiveFrom: row.effectiveFrom.slice(0, 10),
    effectiveTo: row.effectiveTo?.slice(0, 10) ?? null,
    normFormula: normalizeNormFormula(row.normFormula, row.performance),
    deviationFormula: normalizeDeviationFormula(row.deviationFormula, row.performance, row.standardDeviation),
  };
}

export function useIicNormMutations() {
  const queryClient = useQueryClient();
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();

  const invalidateIicNormQueries = useCallback(async () => {
    clearDictionariesHttpCache();
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.registers.iicNormsRoot }),
      queryClient.invalidateQueries({ queryKey: queryKeys.analytics.root }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dictionaries.all() }),
    ]);
  }, [queryClient]);

  const createNormMutation = useMutation({
    mutationFn: (body: IicNormWriteRequest) => createIicNorm(body),
    onSuccess: () => invalidateIicNormQueries(),
  });

  const deleteNormMutation = useMutation({
    mutationFn: (id: number) => deleteIicNorm(id),
    onSuccess: () => invalidateIicNormQueries(),
  });

  const mutating = createNormMutation.isPending || deleteNormMutation.isPending;

  const createNorm = async (
    form: IicNormWriteRequest,
    directions: DictionaryItem[],
  ): Promise<void> => {
    const unitId = resolveIicDirectionUnitId(form.workDirectionId, directions);
    const next = applyNormFormulaFields(
      form.normFormula ?? defaultNormFormula(form.performance ?? 0),
      form.deviationFormula ?? defaultDeviationFormula(),
    );
    const kim = isKimCurveType(next.normFormula.type);
    await createNormMutation.mutateAsync({
      ...form,
      unitId,
      normFormula: next.normFormula,
      deviationFormula: next.deviationFormula,
      performance: next.performance,
      estimate: kim ? next.estimate : form.estimate,
      standardDeviation: kim ? next.standardDeviation : form.standardDeviation,
    });
  };

  const deleteNorm = async (id: number): Promise<boolean> => {
    if (!(await confirm({ title: 'Удалить норму?', description: 'Удалить норму?' }))) return false;
    await deleteNormMutation.mutateAsync(id);
    return true;
  };

  const deleteHistory = async (selectedHistory: IicNormHistoryItem | null): Promise<boolean> => {
    if (!selectedHistory?.id) return false;
    if (!(await confirm({
      title: 'Удалить запись истории?',
      description: 'Удалить запись истории?',
    }))) return false;
    await deleteNormMutation.mutateAsync(selectedHistory.id);
    return true;
  };

  return {
    invalidateIicNormQueries,
    createNormMutation,
    deleteNormMutation,
    mutating,
    createNorm,
    deleteNorm,
    deleteHistory,
    ConfirmDialog,
  };
}
