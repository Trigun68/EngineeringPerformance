import { useCallback, useEffect, useState } from 'react';
import {
  Avatar,
  Box,
  Divider,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Typography,
} from '@mui/material';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined';
import { fetchMe, type CurrentUserProfile } from '../../api/usersMeApi';
import { useAuthStore } from '../../store/useAuthStore';
import { wpColors } from '../../theme/wpColors';
import { ChangePasswordDialog } from './ChangePasswordDialog';

export function UserMenu() {
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [profile, setProfile] = useState<CurrentUserProfile | null>(null);
  const [passwordOpen, setPasswordOpen] = useState(false);

  const menuOpen = Boolean(anchorEl);

  const loadProfile = useCallback(async () => {
    try {
      setProfile(await fetchMe());
    } catch {
      setProfile(null);
    }
  }, []);

  useEffect(() => {
    if (user) {
      void loadProfile();
    }
  }, [user, loadProfile]);

  if (!user) return null;

  const displayName = profile?.displayName?.trim() || '';
  const showNameBlock = displayName.length > 0;

  return (
    <>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          cursor: 'pointer',
          borderRadius: 1,
          px: 0.5,
          py: 0.25,
          '&:hover': { bgcolor: wpColors.pageBg },
        }}
        onClick={(e) => setAnchorEl(e.currentTarget)}
        role="button"
        aria-label="Меню пользователя"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            setAnchorEl(e.currentTarget);
          }
        }}
      >
        <Avatar sx={{ width: 36, height: 36, bgcolor: wpColors.accent, fontSize: 14 }}>
          {(displayName.charAt(0) || user.displayName?.charAt(0) || '?').toUpperCase()}
        </Avatar>
        {showNameBlock && (
          <Box>
            <Typography sx={{ color: wpColors.heading, fontSize: 14, fontWeight: 600, lineHeight: 1.2 }}>
              {displayName}
            </Typography>
            <Typography sx={{ color: wpColors.muted, fontSize: 12 }}>
              {profile?.role || user.roleName}
            </Typography>
          </Box>
        )}
      </Box>

      <Menu
        anchorEl={anchorEl}
        open={menuOpen}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { minWidth: 220, mt: 0.5 } } }}
      >
        <MenuItem
          onClick={() => {
            setAnchorEl(null);
            setPasswordOpen(true);
          }}
        >
          <ListItemIcon>
            <LockOutlinedIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText primary="Сменить пароль" />
        </MenuItem>
        <Divider />
        <MenuItem
          onClick={() => {
            setAnchorEl(null);
            void logout();
          }}
        >
          <ListItemIcon>
            <LogoutOutlinedIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText primary="Выход" />
        </MenuItem>
      </Menu>

      <ChangePasswordDialog open={passwordOpen} onClose={() => setPasswordOpen(false)} />
    </>
  );
}
