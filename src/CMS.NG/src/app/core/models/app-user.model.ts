export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  /** ISO datetime, read-only. Dapper returns Kind=Unspecified — append 'Z' before display. */
  passwordUpdatedTime?: string | null;
  roleCount: number;
  roleIds: string[];
}

/** Write DTO — no password field of any kind (set server-side). */
export interface AppUserRequest {
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
}

export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
}
