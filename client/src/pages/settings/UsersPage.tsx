import { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import {
  archiveUser,
  createUser,
  deleteUser,
  fetchRoles,
  fetchUsers,
  resetUserMfa,
  resetUserPassword,
  unlockUser,
  unarchiveUser,
  updateUser,
} from '../../api/usersApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { DirectoryToolbar } from '../references/DirectoryToolbar';
import { usePermission } from '../../hooks/usePermission';
import { useConfirmation } from '../../hooks/useConfirmation';
import type { AppUser, RoleOption, UserFormData } from '../../types/user';
import { emptyUserForm, UserEditDialog, userToForm } from './UserEditDialog';

const OBJECT_NAME = 'Users';

function boolLabel(value: boolean): string {
  return value ? 'Да' : 'Нет';
}

export function UsersPage() {
  const { canRead, canEdit } = usePermission(OBJECT_NAME);
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();
  const [rows, setRows] = useState<AppUser[]>([]);
  const [roles, setRoles] = useState<RoleOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showArchived, setShowArchived] = useState(false);
  const [selected, setSelected] = useState<AppUser | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AppUser | null>(null);
  const [form, setForm] = useState<UserFormData>(emptyUserForm());
  const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null);

  useEffect(() => {
    if (!canRead) return;
    void fetchRoles()
      .then(setRoles)
      .catch(() => setError('Не удалось загрузить роли.'));
  }, [canRead]);

  const load = useCallback(async () => {
    if (!canRead) return;
    setLoading(true);
    setError(null);
    try {
      setRows(await fetchUsers(showArchived));
    } catch {
      setError('Не удалось загрузить пользователей.');
    } finally {
      setLoading(false);
    }
  }, [canRead, showArchived]);

  useEffect(() => {
    void load();
  }, [load]);

  const columnDefs = useMemo<ColDef<AppUser>[]>(
    () => [
      { field: 'username', headerName: 'Логин', minWidth: 140 },
      { field: 'displayName', headerName: 'Имя', minWidth: 180 },
      { field: 'roleName', headerName: 'Роль', minWidth: 160 },
      {
        field: 'isActive',
        headerName: 'Активный',
        maxWidth: 110,
        valueFormatter: (p) => boolLabel(Boolean(p.value)),
      },
      { field: 'windowsUsername', headerName: 'Windows', minWidth: 140 },
      {
        field: 'isArchived',
        headerName: 'Архив',
        maxWidth: 90,
        valueFormatter: (p) => boolLabel(Boolean(p.value)),
      },
    ],
    [],
  );

  const openCreate = () => {
    setEditing(null);
    setForm(emptyUserForm(roles[0]?.id ?? ''));
    setModalOpen(true);
  };

  const openEdit = (user: AppUser) => {
    setEditing(user);
    setForm(userToForm(user));
    setModalOpen(true);
  };

  const handleSave = async (data: UserFormData) => {
    if (editing) {
      await updateUser(editing.id, data);
    } else {
      await createUser(data);
    }
    await load();
  };

  const handleDelete = async () => {
    if (!selected || !canEdit) return;
    if (!(await confirm({
      title: 'Удалить пользователя?',
      description: `Удалить пользователя «${selected.displayName}»?`,
    }))) return;
    try {
      await deleteUser(selected.id);
      setSelected(null);
      await load();
    } catch {
      setError('Не удалось удалить пользователя. Возможно, есть связанные записи.');
    }
  };

  const handleArchive = async () => {
    if (!selected || !canEdit) return;
    try {
      await archiveUser(selected.id);
      setSelected(null);
      await load();
    } catch {
      setError('Не удалось отправить пользователя в архив.');
    }
  };

  const handleUnarchive = async () => {
    if (!selected || !canEdit) return;
    try {
      await unarchiveUser(selected.id);
      setSelected(null);
      await load();
    } catch {
      setError('Не удалось восстановить пользователя из архива.');
    }
  };

  const handleUnlock = async () => {
    if (!selected || !canEdit) return;
    try {
      await unlockUser(selected.id);
      setError(null);
    } catch {
      setError('Не удалось разблокировать пользователя.');
    }
  };

  const handleResetMfa = async () => {
    if (!selected || !canEdit) return;
    if (!(await confirm({
      title: 'Сбросить MFA?',
      description: `Сбросить MFA для пользователя «${selected.displayName}»?`,
    }))) return;
    try {
      await resetUserMfa(selected.id);
      setError(null);
    } catch {
      setError('Не удалось сбросить MFA. Возможно, MFA отключена.');
    }
  };

  const handleResetPassword = async () => {
    if (!selected || !canEdit) return;
    if (!(await confirm({
      title: 'Сбросить пароль?',
      description: `Сбросить пароль пользователя «${selected.displayName}»?`,
    }))) return;
    try {
      setTemporaryPassword(await resetUserPassword(selected.id));
      setError(null);
    } catch {
      setError('Не удалось сбросить пароль пользователя.');
    }
  };

  if (!canRead) {
    return <Alert severity="warning">Недостаточно прав для просмотра пользователей.</Alert>;
  }

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Пользователи
      </Typography>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
      <DirectoryToolbar
        canEdit={canEdit}
        hasSelection={selected !== null}
        showExcel={false}
        showArchived={showArchived}
        onShowArchivedChange={setShowArchived}
        onAdd={openCreate}
        onEdit={() => selected && openEdit(selected)}
        onDelete={() => void handleDelete()}
        onArchive={() => void handleArchive()}
        onUnarchive={() => void handleUnarchive()}
        onRefresh={() => void load()}
        onExport={() => undefined}
      />
      {canEdit && (
        <Stack direction="row" spacing={1} sx={{ mb: 1, flexWrap: 'wrap' }}>
          <Button size="small" variant="outlined" disabled={!selected} onClick={() => void handleUnlock()}>
            Разблокировать
          </Button>
          <Button size="small" variant="outlined" disabled={!selected} onClick={() => void handleResetMfa()}>
            Сбросить MFA
          </Button>
          <Button size="small" variant="outlined" disabled={!selected} onClick={() => void handleResetPassword()}>
            Сбросить пароль
          </Button>
        </Stack>
      )}
      <DataTable
        gridId="users"
        objectName={OBJECT_NAME}
        rowData={rows}
        columnDefs={columnDefs}
        loading={loading}
        onRowClicked={(row) => setSelected(row)}
        onRowDoubleClicked={(row) => canEdit && openEdit(row)}
      />
      <UserEditDialog
        open={modalOpen}
        title={editing ? 'Редактирование пользователя' : 'Новый пользователь'}
        roles={roles}
        initial={form}
        isEditMode={editing !== null}
        onClose={() => setModalOpen(false)}
        onSubmit={handleSave}
      />
      <Dialog open={temporaryPassword !== null} onClose={() => setTemporaryPassword(null)} fullWidth maxWidth="xs">
        <DialogTitle>Временный пароль</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 2 }}>
            Передайте пароль пользователю защищённым способом. При следующем входе он должен будет сменить его.
          </Typography>
          <TextField label="Временный пароль" value={temporaryPassword ?? ''} fullWidth InputProps={{ readOnly: true }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTemporaryPassword(null)}>Закрыть</Button>
        </DialogActions>
      </Dialog>
      {ConfirmDialog}
    </Box>
  );
}
