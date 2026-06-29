import { create } from 'zustand';
import {
  fetchPermissionMatrix,
  type PermissionMatrix,
} from '../api/permissionsMatrixApi';

interface PermissionMatrixState {
  matrix: PermissionMatrix | null;
  loading: boolean;
  error: string | null;
  loadedForUserId: number | null;
  fetchMatrix: (userId: number, isAdmin: boolean) => Promise<void>;
  clear: () => void;
}

export const usePermissionMatrixStore = create<PermissionMatrixState>((set, get) => ({
  matrix: null,
  loading: false,
  error: null,
  loadedForUserId: null,

  fetchMatrix: async (userId, isAdmin) => {
    if (!isAdmin) {
      set({ matrix: null, loadedForUserId: userId, error: null });
      return;
    }
    if (get().loadedForUserId === userId && get().matrix) return;

    set({ loading: true, error: null });
    try {
      const matrix = await fetchPermissionMatrix();
      set({ matrix, loadedForUserId: userId, loading: false });
    } catch {
      set({
        matrix: null,
        loadedForUserId: null,
        loading: false,
        error: 'Не удалось загрузить матрицу прав.',
      });
    }
  },

  clear: () => set({ matrix: null, loadedForUserId: null, loading: false, error: null }),
}));
