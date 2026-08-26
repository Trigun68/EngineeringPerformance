export type AccessLevel = 0 | 1 | 2;

export interface AccessRight {
  objectName: string;
  accessLevel: AccessLevel;
}

export interface AuthUser {
  id: number;
  username: string;
  displayName: string;
  roleId: number;
  roleName: string;
  windowsUsername?: string | null;
  isActive: boolean;
  mfaEnabled: boolean;
  mustChangePassword: boolean;
  permissions: Record<string, AccessLevel>;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  expiresAt: string;
  user: AuthUser;
  requiresMfa?: boolean;
  mfaSessionToken?: string | null;
  requiresMfaSetup?: boolean;
}

export interface MfaSetupResponse {
  secret: string;
  qrCodeUri: string;
}

export interface MfaStatus {
  isEnabled: boolean;
  isRequired: boolean;
}
