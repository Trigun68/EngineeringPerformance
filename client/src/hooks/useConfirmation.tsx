import { useCallback, useState } from 'react';
import { ConfirmDialog } from '../features/projects/shared/ConfirmDialog';

export interface ConfirmationOptions {
  title: string;
  description: string;
  confirmText?: string;
  cancelText?: string;
}

/**
 * Хук для управления диалогом подтверждения.
 * Возвращает функцию `confirm` и компонент `<ConfirmDialog>`.
 * Используется вместо `window.confirm`.
 */
export function useConfirmation() {
  const [state, setState] = useState<{
    open: boolean;
    options: ConfirmationOptions;
    resolve: ((value: boolean) => void) | null;
  }>({ open: false, options: { title: '', description: '' }, resolve: null });

  const confirm = useCallback((options: ConfirmationOptions): Promise<boolean> => {
    return new Promise((resolve) => {
      setState({ open: true, options, resolve });
    });
  }, []);

  const handleClose = useCallback(() => {
    state.resolve?.(false);
    setState((prev) => ({ ...prev, open: false, resolve: null }));
  }, [state.resolve]);

  const handleConfirm = useCallback(() => {
    state.resolve?.(true);
    setState((prev) => ({ ...prev, open: false, resolve: null }));
  }, [state.resolve]);

  const Dialog = state.open ? (
    <ConfirmDialog
      open
      title={state.options.title}
      description={state.options.description}
      confirmText={state.options.confirmText}
      cancelText={state.options.cancelText}
      onConfirm={handleConfirm}
      onCancel={handleClose}
    />
  ) : null;

  return { confirm, Dialog };
}
