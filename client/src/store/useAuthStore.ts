import { create } from 'zustand';
import { isAxiosError } from 'axios';
import apiClient, {
  broadcastLogout,
  setForbiddenHandler,
  setRemoteLogoutHandler,
  setUnauthorizedHandler,
} from '../services/apiClient';
import { getMfaStatus, verifyMfa as verifyMfaRequest } from '../api/authApi';
import { getApiErrorMessage } from '../services/apiError';
import type { AuthUser, LoginRequest, LoginResponse } from '../types/auth';
import { usePermissionMatrixStore } from './usePermissionMatrixStore';
import { isSuperAdmin } from '../utils/permissionAccess';

interface AuthState {
  user: AuthUser | null;
  status: 'unauthenticated' | 'mfa_required' | 'authenticated';
  mfaSessionToken: string | null;
  requiresMfaSetup: boolean;
  isLoading: boolean;
  isInitialized: boolean;
  error: string | null;
  showLoginForm: boolean;
  initialize: () => Promise<void>;
  login: (credentials: LoginRequest) => Promise<void>;
  verifyMfa: (code: string) => Promise<void>;
  completeMfaSetup: () => void;
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
        status: 'unauthenticated',
        mfaSessionToken: null,
        requiresMfaSetup: false,
        showLoginForm: true,
        error: 'Сессия истекла. Войдите снова.',
      });
    }
  });

  setForbiddenHandler((message) => {
    set({ error: message ?? 'Недостаточно прав для выполнения операции.' });
  });

  setRemoteLogoutHandler(() => {
    usePermissionMatrixStore.getState().clear();
    set({
      user: null,
      status: 'unauthenticated',
      mfaSessionToken: null,
      requiresMfaSetup: false,
      showLoginForm: true,
      error: null,
    });
  });

  return {
    user: null,
    status: 'unauthenticated',
    mfaSessionToken: null,
    requiresMfaSetup: false,
    isLoading: false,
    isInitialized: false,
    error: null,
    showLoginForm: false,

    initialize: async () => {
      set({ isLoading: true, error: null });
      try {
        // Проверяем активную сессию.
        try {
          const { data } = await apiClient.get<AuthUser>('/auth/me');
          const user = { ...data, permissions: buildPermissions(data.permissions) };
          const mfaStatus = await getMfaStatus();
          set({
            user,
            status: 'authenticated',
            requiresMfaSetup: mfaStatus.isRequired && !mfaStatus.isEnabled,
            showLoginForm: false,
          });
          void usePermissionMatrixStore.getState().fetchMatrix(
            user.id,
            isSuperAdmin(user.roleName, user.roleId),
          );
          return;
        } catch {
          // Сессии нет — пробуем Windows auto-login.
        }

        try {
          const { data } = await apiClient.get<LoginResponse>('/auth/negotiate');
          if (data.requiresMfa && data.mfaSessionToken) {
            set({
              status: 'mfa_required',
              mfaSessionToken: data.mfaSessionToken,
              showLoginForm: true,
            });
            return;
          }
          // Negotiate вернул пользователя — устанавливаем сессию.
          const user = { ...data.user, permissions: buildPermissions(data.user.permissions) };
          set({
            user,
            status: 'authenticated',
            requiresMfaSetup: data.requiresMfaSetup === true,
            showLoginForm: false,
          });
          void usePermissionMatrixStore.getState().fetchMatrix(
            user.id,
            isSuperAdmin(user.roleName, user.roleId),
          );
          return;
        } catch (error) {
          if (isAxiosError(error) && error.response?.status === 404) {
            set({ user: null, status: 'unauthenticated', showLoginForm: true });
            return;
          }
          // Windows auto-login недоступен — показываем форму логина.
        }

        set({ user: null, status: 'unauthenticated', showLoginForm: true });
      } catch {
        set({ user: null, status: 'unauthenticated', showLoginForm: true });
      } finally {
        set({ isLoading: false, isInitialized: true });
      }
    },

    login: async (credentials) => {
      set({ isLoading: true, error: null });
      try {
        const { data } = await apiClient.post<LoginResponse>('/auth/login', credentials);
        if (data.requiresMfa && data.mfaSessionToken) {
          set({
            user: null,
            status: 'mfa_required',
            mfaSessionToken: data.mfaSessionToken,
            requiresMfaSetup: false,
            showLoginForm: true,
          });
          return;
        }

        const user = { ...data.user, permissions: buildPermissions(data.user.permissions) };
        set({
          user,
          status: 'authenticated',
          mfaSessionToken: null,
          requiresMfaSetup: data.requiresMfaSetup === true,
          showLoginForm: false,
        });
        void usePermissionMatrixStore.getState().fetchMatrix(
          user.id,
          isSuperAdmin(user.roleName, user.roleId),
        );
      } catch (err) {
        set({ error: getApiErrorMessage(err, 'Неверный логин или пароль.') });
        throw new Error('Login failed');
      } finally {
        set({ isLoading: false });
      }
    },

    verifyMfa: async (code) => {
      const mfaSessionToken = get().mfaSessionToken;
      if (!mfaSessionToken) {
        set({ error: 'Сессия подтверждения MFA истекла. Войдите снова.' });
        return;
      }

      set({ isLoading: true, error: null });
      try {
        const data = await verifyMfaRequest(code, mfaSessionToken);
        const user = { ...data.user, permissions: buildPermissions(data.user.permissions) };
        set({
          user,
          status: 'authenticated',
          mfaSessionToken: null,
          requiresMfaSetup: data.requiresMfaSetup === true,
          showLoginForm: false,
        });
        void usePermissionMatrixStore.getState().fetchMatrix(
          user.id,
          isSuperAdmin(user.roleName, user.roleId),
        );
      } catch (err) {
        set({ error: getApiErrorMessage(err, 'Неверный код MFA.') });
        throw new Error('MFA verification failed');
      } finally {
        set({ isLoading: false });
      }
    },

    completeMfaSetup: () => set({ requiresMfaSetup: false }),

    logout: async () => {
      try {
        await apiClient.post('/auth/logout');
      } catch {
        // cookie may already be cleared
      }
      broadcastLogout();
      usePermissionMatrixStore.getState().clear();
      set({
        user: null,
        status: 'unauthenticated',
        mfaSessionToken: null,
        requiresMfaSetup: false,
        showLoginForm: true,
        error: null,
      });
    },

    refreshUser: async () => {
      try {
        const { data } = await apiClient.get<AuthUser>('/auth/me');
        const user = { ...data, permissions: buildPermissions(data.permissions) };
        set({ user, status: 'authenticated' });
        void usePermissionMatrixStore.getState().fetchMatrix(
          user.id,
          isSuperAdmin(user.roleName, user.roleId),
        );
      } catch {
        // Сессия могла истечь — обработчик 401 сработает отдельно.
      }
    },

    clearError: () => set({ error: null }),
  };
});
