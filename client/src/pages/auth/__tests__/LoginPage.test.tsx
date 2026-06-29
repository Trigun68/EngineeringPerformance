import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginPage } from '../LoginPage';

const fetchWindowsIdentityHint = vi.fn();
const login = vi.fn();
const clearError = vi.fn();

vi.mock('../../../api/authApi', () => ({
  fetchWindowsIdentityHint: (...args: unknown[]) => fetchWindowsIdentityHint(...args),
}));

vi.mock('../../../store/useAuthStore', () => ({
  useAuthStore: (selector: (s: Record<string, unknown>) => unknown) =>
    selector({
      login,
      isLoading: false,
      error: null,
      clearError,
    }),
}));

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows neutral text when identity hint is empty', async () => {
    fetchWindowsIdentityHint.mockResolvedValue({
      windowsUsername: null,
      displayName: null,
      source: 'none',
    });

    render(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByText('Введите учетные данные')).toBeInTheDocument();
    });
    expect(screen.queryByText(/LAPTOP-ANTON/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Windows-пользователь/i)).not.toBeInTheDocument();
  });

  it('shows welcome text from API display name', async () => {
    fetchWindowsIdentityHint.mockResolvedValue({
      windowsUsername: 'DOMAIN\\ivanov',
      displayName: 'Иванов И.И.',
      source: 'proxy',
    });

    render(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByText('Добро пожаловать, Иванов И.И.')).toBeInTheDocument();
    });
  });

  it('ignores development simulation hints on UI', async () => {
    fetchWindowsIdentityHint.mockResolvedValue({
      windowsUsername: 'LAPTOP-ANTON\\elise',
      displayName: null,
      source: 'development',
    });

    render(<LoginPage />);

    await waitFor(() => {
      expect(screen.getByText('Введите учетные данные')).toBeInTheDocument();
    });
    expect(screen.queryByText(/LAPTOP-ANTON/i)).not.toBeInTheDocument();
  });
});
