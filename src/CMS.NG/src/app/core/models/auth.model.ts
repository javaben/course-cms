// Credentials posted to POST /api/Auth/login.
export interface LoginRequest {
  userId: string;
  password: string;
}

// Profile returned on success and stored in session storage. Roles are NOT a field here —
// they are carried inside the accessToken (a JWT) and decoded on demand.
export interface AuthProfile {
  userId: string;
  userName: string;
  accessToken: string;
}

// Result of PUT /api/Auth/profile — the (trimmed) UserName for the authenticated user.
export interface ProfileResponse {
  userId: string;
  userName: string;
}

// Body for POST /api/Auth/change-password. All plaintext; hashed server-side, never a hash.
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}
