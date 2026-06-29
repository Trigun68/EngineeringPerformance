import { create } from 'zustand';
import apiClient, { setForbiddenHandler, setUnauthorizedHandler } from '../services/apiClient';
import { getApiErrorMessage } from '../services/apiError';
import type { AuthUser, LoginRequest, LoginResponse } from '../types/auth';
import { usePermissionMatrixStore } from './usePermissionMatrixStore';

interface AuthState {
  user: AuthUser | null;
  isLoading: boolean;
  isInitialized: boolean;
  error: string | null;
  showLoginForm: boolean;
  initialize: () => Promise<void>;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  clearError: () => void;
}

function buildPermissions(rights: AuthUser['permissions'] | undefined): Record<string, 0 | 1 | 2> {
  return rights ?? {};
}

export const useAuthStore = create<AuthState>((set, get) => {
  setUnauthorizedHandler(() => {
    const { user } = get();
    if (user) {
      set({
        user: null,
        showLoginForm: true,
        error: 'Сессия истекла. Войдите снова.',
      });
    }
  });

  setForbiddenHandler((message) => {
    set({ error: message ?? 'Недостаточно прав для выполнения операции.' });
  });

  return {
    user: null,
    isLoading: false,
    isInitialized: false,
    error: null,
    showLoginForm: false,

    initialize: async () => {
      set({ isLoading: true, error: null });
      try {
        // Сначала проверяем активную сессию — negotiate не должен перезаписывать JWT.
        try {
          const { data } = await apiClient.get<AuthUser>('/auth/me');
          const user = { ...data, permissions: buildPermissions(data.permissions) };
          set({ user, showLoginForm: false });
          void usePermissionMatrixStore.getState().fetchMatrix(
            user.id,
            user.roleName === 'Administrator',
          );
          return;
        } catch {
          // Сессии нет — пробуем Windows auto-login (SPNEGO / dev simulate).
        }

        try {
          await apiClient.get<LoginResponse>('/auth/negotiate');
        } catch {
          // Windows auto-login недоступен — показываем форму логина/пароля.
        }

        const { data } = await apiClient.get<AuthUser>('/auth/me');
        const user = { ...data, permissions: buildPermissions(data.permissions) };
        set({ user, showLoginForm: false });
        void usePermissionMatrixStore.getState().fetchMatrix(
          user.id,
          user.roleName === 'Administrator',
        );
      } catch {
        set({ user: null, showLoginForm: true });
      } finally {
        set({ isLoading: false, isInitialized: true });
      }
    },

    login: async (credentials) => {
      set({ isLoading: true, error: null });
      try {
        const { data } = await apiClient.post<LoginResponse>('/auth/login', credentials);
        const user = { ...data.user, permissions: buildPermissions(data.user.permissions) };
        set({ user, showLoginForm: false });
        void usePermissionMatrixStore.getState().fetchMatrix(
          user.id,
          user.roleName === 'Administrator',
        );
      } catch (err) {
        set({ error: getApiErrorMessage(err, 'Неверный логин или пароль.') });
        throw new Error('Login failed');
      } finally {
        set({ isLoading: false });
      }
    },

    logout: async () => {
      try {
        await apiClient.post('/auth/logout');
      } catch {
        // cookie may already be cleared
      }
      usePermissionMatrixStore.getState().clear();
      set({ user: null, showLoginForm: true, error: null });
    },

    refreshUser: async () => {
      try {
        const { data } = await apiClient.get<AuthUser>('/auth/me');
        const user = { ...data, permissions: buildPermissions(data.permissions) };
        set({ user });
        void usePermissionMatrixStore.getState().fetchMatrix(
          user.id,
          user.roleName === 'Administrator',
        );
      } catch {
        // Сессия могла истечь — обработчик 401 сработает отдельно.
      }
    },

    clearError: () => set({ error: null }),
  };
});
