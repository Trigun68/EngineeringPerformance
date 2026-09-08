import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { fetchProjectVersions, type ProjectVersion } from '../../api/projectVersionsApi';
import { getApiErrorMessage } from '../../services/apiError';
import { useDictionaryStore } from '../../store/useDictionaryStore';
import { invalidateAfterVersionMutation } from './versionPickerInvalidation';
import {
  ANALYSIS_FACT,
  ANALYSIS_PLAN,
  emptyVersionForm,
  type VersionFormState,
  type VersionPickerDirection,
} from './versionPickerModel';

export interface UseVersionPickerListParams {
  open: boolean;
  projectId: number;
  versionsProp?: ProjectVersion[];
  directionFilter: VersionPickerDirection;
  selectedVersionId?: number | null;
  onVersionsChanged?: () => void;
}

export function useVersionPickerList({
  open,
  projectId,
  versionsProp,
  directionFilter,
  selectedVersionId,
  onVersionsChanged,
}: UseVersionPickerListParams) {
  const queryClient = useQueryClient();
  const [versions, setVersions] = useState<ProjectVersion[]>(versionsProp ?? []);
  const [selected, setSelected] = useState<ProjectVersion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [editOpen, setEditOpen] = useState(false);
  const [editMode, setEditMode] = useState<'create' | 'edit'>('create');
  const [form, setForm] = useState<VersionFormState>(emptyVersionForm);
  const [copyOpen, setCopyOpen] = useState(false);
  const [copyMode, setCopyMode] = useState<'same' | 'from-plan'>('same');
  const [copyName, setCopyName] = useState('');
  const [copyDescription, setCopyDescription] = useState('');
  const [planSource, setPlanSource] = useState<ProjectVersion | null>(null);
  const physicalVolumeTypes = useDictionaryStore((s) => s.lists.physicalVolumeTypes);
  const catalogOptions = useMemo(
    () => physicalVolumeTypes.filter((t) => t.name === 'КФО' || t.name === 'БЕР' || t.name === 'БЭР'),
    [physicalVolumeTypes],
  );
  const [submitting, setSubmitting] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const userPickedIdRef = useRef<number | null>(null);
  const wasOpenRef = useRef(false);
  const [actionTarget, setActionTarget] = useState<ProjectVersion | null>(null);

  const defaultDirection = directionFilter === 'any' ? ANALYSIS_PLAN : directionFilter;
  const allowCopyFromPlan = directionFilter === ANALYSIS_FACT;

  const pickVersion = useCallback((version: ProjectVersion | null, fromUser = false) => {
    if (fromUser) {
      userPickedIdRef.current = version?.id ?? null;
    }
    setSelected(version);
  }, []);

  const loadVersions = useCallback(async () => {
    try {
      const list = await fetchProjectVersions(projectId);
      setVersions(list);
      onVersionsChanged?.();
      return list;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось загрузить версии.'));
      return [] as ProjectVersion[];
    }
  }, [onVersionsChanged, projectId]);

  const refreshAfterMutation = useCallback(async () => {
    await invalidateAfterVersionMutation(queryClient, projectId);
    return loadVersions();
  }, [loadVersions, projectId, queryClient]);

  useEffect(() => {
    if (!open) return;
    void loadVersions();
  }, [loadVersions, open]);

  const filteredVersions = useMemo(() => {
    if (directionFilter === 'any') return versions;
    return versions.filter((v) => v.direction === directionFilter);
  }, [directionFilter, versions]);

  const planVersions = useMemo(
    () => versions.filter((v) => v.direction === ANALYSIS_PLAN),
    [versions],
  );

  useEffect(() => {
    if (open && !wasOpenRef.current) {
      userPickedIdRef.current = null;
    }
    wasOpenRef.current = open;
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setSelected((prev) => {
      const preferredId = userPickedIdRef.current ?? selectedVersionId ?? null;
      if (preferredId != null) {
        const match = filteredVersions.find((v) => v.id === preferredId);
        if (match) return match;
      }
      if (prev) {
        const still = filteredVersions.find((v) => v.id === prev.id);
        if (still) return still;
      }
      return filteredVersions[0] ?? null;
    });
  }, [filteredVersions, open, selectedVersionId]);

  const clearUserPick = () => {
    userPickedIdRef.current = null;
  };

  return {
    selected,
    error,
    setError,
    editOpen,
    setEditOpen,
    editMode,
    setEditMode,
    form,
    setForm,
    copyOpen,
    setCopyOpen,
    copyMode,
    setCopyMode,
    copyName,
    setCopyName,
    copyDescription,
    setCopyDescription,
    planSource,
    setPlanSource,
    catalogOptions,
    submitting,
    setSubmitting,
    deleteConfirmOpen,
    setDeleteConfirmOpen,
    actionTarget,
    setActionTarget,
    filteredVersions,
    planVersions,
    allowCopyFromPlan,
    defaultDirection,
    pickVersion,
    refreshAfterMutation,
    clearUserPick,
    projectId,
    directionFilter,
  };
}
