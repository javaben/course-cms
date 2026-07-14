export interface AppRole {
  pkid: number;
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description?: string | null;
  userCount: number;
  userIds: string[];
}

export interface AppRoleRequest {
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description?: string | null;
  userIds: string[];
}

export interface AppRoleQuery {
  keyword?: string | null;
  permissionLevel?: number | null;
}
