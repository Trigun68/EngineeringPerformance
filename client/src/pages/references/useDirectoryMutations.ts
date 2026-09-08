import { useCallback } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  archiveDirectoryEntry,
  createDirectoryEntry,
  deleteDirectoryEntry,
  unarchiveDirectoryEntry,
  updateDirectoryEntry,
  type DirectoryEntry,
  type DirectoryWriteRequest,
} from '../../api/directoriesApi';
import { getApiErrorMessage } from '../../services/apiError';
import { queryKeys } from '../../services/queryKeys';

/** Префикс ключа списка справочника — без showArchived/projectId, чтобы сбросить все срезы. */
export function directoryPageQueryKey(resource: string) {
  return queryKeys.directory.root(resource);
}

export interface DirectorySubmitVariables {
  editing: DirectoryEntry | null;
  body: DirectoryWriteRequest;
  formIsArchived: boolean;
}

interface UseDirectoryMutationsOptions {
  resource: string;
  invalidateDictionaries: () => void | Promise<void>;
  onError: (message: string) => void;
}

async function submitDirectoryEntry(
  resource: string,
  { editing, body, formIsArchived }: DirectorySubmitVariables,
): Promise<void> {
  if (editing) {
    await updateDirectoryEntry(resource, editing.id, body);
    if (formIsArchived && !editing.isArchived) {
      await archiveDirectoryEntry(resource, editing.id);
    } else if (!formIsArchived && editing.isArchived) {
      await unarchiveDirectoryEntry(resource, editing.id);
    }
    return;
  }

  const created = await createDirectoryEntry(resource, body);
  if (formIsArchived) {
    await archiveDirectoryEntry(resource, created.id);
  }
}

export function useDirectoryMutations({
  resource,
  invalidateDictionaries,
  onError,
}: UseDirectoryMutationsOptions) {
  const queryClient = useQueryClient();

  const invalidateAfterWrite = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: directoryPageQueryKey(resource) });
    await invalidateDictionaries();
  }, [queryClient, resource, invalidateDictionaries]);

  const submitMutation = useMutation({
    mutationFn: (vars: DirectorySubmitVariables) => submitDirectoryEntry(resource, vars),
    onSuccess: () => invalidateAfterWrite(),
    onError: (err) => onError(getApiErrorMessage(err, 'Не удалось сохранить запись.')),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteDirectoryEntry(resource, id),
    onSuccess: () => invalidateAfterWrite(),
    onError: (err) => onError(getApiErrorMessage(err, 'Не удалось удалить запись.')),
  });

  const archiveMutation = useMutation({
    mutationFn: (id: number) => archiveDirectoryEntry(resource, id),
    onSuccess: () => invalidateAfterWrite(),
    onError: (err) => onError(getApiErrorMessage(err, 'Не удалось архивировать запись.')),
  });

  const unarchiveMutation = useMutation({
    mutationFn: (id: number) => unarchiveDirectoryEntry(resource, id),
    onSuccess: () => invalidateAfterWrite(),
    onError: (err) => onError(getApiErrorMessage(err, 'Не удалось разархивировать запись.')),
  });

  const isMutating =
    submitMutation.isPending
    || deleteMutation.isPending
    || archiveMutation.isPending
    || unarchiveMutation.isPending;

  return {
    submitMutation,
    deleteMutation,
    archiveMutation,
    unarchiveMutation,
    isMutating,
  };
}
