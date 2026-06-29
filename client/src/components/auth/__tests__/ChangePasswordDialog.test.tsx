import { createTheme, ThemeProvider } from '@mui/material';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ChangePasswordDialog } from '../ChangePasswordDialog';

vi.mock('../../../api/usersMeApi', () => ({
  changeOwnPassword: vi.fn().mockResolvedValue(undefined),
}));

const theme = createTheme();

function renderDialog(onClose = () => undefined) {
  return render(
    <ThemeProvider theme={theme}>
      <ChangePasswordDialog open onClose={onClose} />
    </ThemeProvider>,
  );
}

describe('ChangePasswordDialog', () => {
  it('validates and submits password change', async () => {
    const onClose = vi.fn();
    renderDialog(onClose);

    const dialog = screen.getByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText(/Текущий пароль/i), { target: { value: 'old-password' } });
    fireEvent.change(within(dialog).getByLabelText(/Новый пароль/i), { target: { value: 'new-password-1' } });
    fireEvent.change(within(dialog).getByLabelText(/Подтверждение пароля/i), {
      target: { value: 'new-password-1' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Сохранить' }));

    await waitFor(() => expect(onClose).toHaveBeenCalled());
  });

  it('shows validation error for short password', async () => {
    renderDialog();

    const dialog = screen.getByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText(/Текущий пароль/i), { target: { value: 'old-password' } });
    fireEvent.change(within(dialog).getByLabelText(/Новый пароль/i), { target: { value: 'short' } });
    fireEvent.change(within(dialog).getByLabelText(/Подтверждение пароля/i), { target: { value: 'short' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Сохранить' }));

    expect(await within(dialog).findByText(/не менее 8 символов/i)).toBeInTheDocument();
  });
});
