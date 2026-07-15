import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '@environments/environment';
import { LoginRequest, ProfileResponse, UserProfile } from '@core/models/auth.model';

/**
 * The role claim in the JWT is written by the backend as ClaimTypes.Role (the WS-* URI).
 * We also accept the short "role"/"roles" forms for robustness.
 */
const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const STORAGE_KEY = 'cms.auth';

/**
 * Holds the signed-in profile in <b>session storage</b> (cleared when the tab closes) and exposes it
 * reactively. Roles are decoded from the stored access token — no separate API call.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  /** Reactive current profile; initialised from session storage so a page reload stays signed in. */
  readonly profile = signal<UserProfile | null>(readProfile());

  readonly isAuthenticated = computed(() => this.profile() !== null);
  readonly userName = computed(() => this.profile()?.userName ?? '');
  readonly roles = computed(() => decodeRoles(this.profile()?.accessToken ?? null));
  readonly isAdmin = computed(() => this.roles().includes('Admin'));

  /** POST the credentials; on success persist the profile to session storage. */
  login(request: LoginRequest): Observable<UserProfile> {
    return this.http
      .post<UserProfile>(`${environment.apiUrl}/Auth/login`, request)
      .pipe(tap((profile) => this.setSession(profile)));
  }

  /**
   * Update the signed-in user's own display name (the backend resolves the account from the JWT).
   * On success, refresh the stored profile so the shell reflects the new name immediately.
   */
  updateProfile(userName: string): Observable<ProfileResponse> {
    return this.http
      .put<ProfileResponse>(`${environment.apiUrl}/Auth/profile`, { userName })
      .pipe(tap((response) => this.applyUserName(response.userName)));
  }

  /**
   * Change the signed-in user's own password (the backend resolves the account from the JWT and
   * verifies the current password). Only plaintext fields are sent — no password hash ever leaves or
   * enters the client. Nothing local changes: the existing token/session stays valid.
   */
  changePassword(
    currentPassword: string,
    newPassword: string,
    confirmNewPassword: string,
  ): Observable<{ message?: string }> {
    return this.http.post<{ message?: string }>(`${environment.apiUrl}/Auth/change-password`, {
      currentPassword,
      newPassword,
      confirmNewPassword,
    });
  }

  /** Clear session storage and drop the in-memory profile. */
  logout(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.profile.set(null);
  }

  /** The raw access token, read live from session storage (used by the HTTP interceptor). */
  get token(): string | null {
    return readProfile()?.accessToken ?? null;
  }

  private setSession(profile: UserProfile): void {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
    this.profile.set(profile);
  }

  /** Replace only the userName on the stored profile (token + userId untouched). */
  private applyUserName(userName: string): void {
    const current = this.profile();
    if (!current) return;
    this.setSession({ ...current, userName });
  }
}

function readProfile(): UserProfile | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as UserProfile;
  } catch {
    return null;
  }
}

/** Decode the JWT payload and extract role claims (accepts a single string or an array). */
export function decodeRoles(token: string | null): string[] {
  if (!token) return [];
  const segments = token.split('.');
  if (segments.length < 2) return [];
  try {
    const payload = JSON.parse(base64UrlDecode(segments[1])) as Record<string, unknown>;
    const claim = payload[ROLE_CLAIM_URI] ?? payload['role'] ?? payload['roles'];
    if (Array.isArray(claim)) return claim.map((r) => String(r));
    if (typeof claim === 'string') return [claim];
    return [];
  } catch {
    return [];
  }
}

function base64UrlDecode(value: string): string {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  return atob(padded);
}
