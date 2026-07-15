import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import {
  AuthProfile,
  ChangePasswordRequest,
  LoginRequest,
  ProfileResponse,
} from '@core/models/auth.model';

/**
 * Owns the signed-in session. The profile (userId, userName, accessToken) lives in **session
 * storage** — not local storage — so it clears when the tab closes. Roles are decoded from the
 * JWT's `role` claim; there is no separate roles endpoint.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/Auth`;

  static readonly STORAGE_KEY = 'auth-profile';

  /** The current session profile, or null when signed out. Reactive for the shell/menu. */
  readonly profile = signal<AuthProfile | null>(this.readSession());

  /** Roles from the JWT `role` claim (empty when signed out). */
  readonly roles = computed(() => decodeRoles(this.profile()?.accessToken ?? null));

  /** True when the signed-in user's roles include "Admin". */
  readonly isAdmin = computed(() => this.roles().includes('Admin'));

  login(request: LoginRequest): Observable<AuthProfile> {
    return this.http
      .post<AuthProfile>(`${this.baseUrl}/login`, request)
      .pipe(tap((profile) => this.setSession(profile)));
  }

  /**
   * Updates the signed-in user's UserName (server takes the UserId from the JWT). On success the
   * stored profile + shell reflect the canonical (trimmed) name the server returns.
   */
  updateUserName(userName: string): Observable<ProfileResponse> {
    return this.http
      .put<ProfileResponse>(`${this.baseUrl}/profile`, { userName })
      .pipe(
        tap((res) => {
          const current = this.profile();
          if (current) this.setSession({ ...current, userName: res.userName });
        }),
      );
  }

  /** Changes the signed-in user's own password (all plaintext in, nothing sensitive back). */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, request);
  }

  /**
   * Admin-only: reset a target user's password to the system default. The client sends only the
   * target UserId and receives only success/failure — never a password or hash.
   */
  resetPasswordToDefault(userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/reset-password`, { userId });
  }

  /** Clears the session (used by logout and on a 401). */
  logout(): void {
    sessionStorage.removeItem(AuthService.STORAGE_KEY);
    this.profile.set(null);
  }

  get token(): string | null {
    return this.profile()?.accessToken ?? null;
  }

  isAuthenticated(): boolean {
    return this.token !== null;
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  private setSession(profile: AuthProfile): void {
    sessionStorage.setItem(AuthService.STORAGE_KEY, JSON.stringify(profile));
    this.profile.set(profile);
  }

  private readSession(): AuthProfile | null {
    const raw = sessionStorage.getItem(AuthService.STORAGE_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthProfile;
    } catch {
      return null;
    }
  }
}

/** Decodes the `role` claim (string or string[]) from a JWT payload. Returns [] on any problem. */
function decodeRoles(token: string | null): string[] {
  if (!token) return [];
  const parts = token.split('.');
  if (parts.length < 2) return [];
  try {
    const json = atob(base64UrlToBase64(parts[1]));
    const payload = JSON.parse(json) as Record<string, unknown>;
    const raw = payload['role'];
    if (Array.isArray(raw)) return raw.map(String);
    if (typeof raw === 'string') return [raw];
    return [];
  } catch {
    return [];
  }
}

function base64UrlToBase64(value: string): string {
  const replaced = value.replace(/-/g, '+').replace(/_/g, '/');
  const pad = replaced.length % 4;
  return pad ? replaced + '='.repeat(4 - pad) : replaced;
}
