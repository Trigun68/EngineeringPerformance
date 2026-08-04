export interface AppUser {
  id: number;
  username: string;
  displayName: string;
  roleId: number;
  roleName: string;
  windowsUsername: string | null;
  isActive: boolean;
  isArchived: boolean;
}

export interface RoleOption {
  id: number;
  name: string;
  description: string | null;
}

export interface UserFormData {
  username: string;
  displayName: string;
  password: string;
  roleId: number | '';
  windowsUsername: string;
  isActive: boolean;
  mustChangePassword: boolean;
}
