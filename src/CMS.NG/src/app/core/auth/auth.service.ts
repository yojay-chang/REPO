import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '@environments/environment';
import {
  ChangePasswordResponse,
  LoginRequest,
  ProfileResponse,
  UserProfile,
} from '@core/models/auth.model';

/**
 * The role claim in the JWT is written by the backend as ClaimTypes.Role (the WS-* URI).
 * We also accept the short "role"/"roles" forms for robustness.
 */
const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const STORAGE_KEY = 'cms.auth';

/** Claim the backend writes when the account still uses the system default password. */
const MUST_CHANGE_PASSWORD_CLAIM = 'mustChangePassword';

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

  /**
   * True while the signed-in account still uses the system default password. Read from the token claim —
   * the same thing the backend enforces — so it stays correct after a reload and clears the moment
   * `changePassword` swaps in the fresh token. The guard uses it to force the change-password page.
   */
  readonly mustChangePassword = computed(() =>
    decodeMustChangePassword(this.profile()?.accessToken ?? null),
  );

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
   * enters the client. On success the server returns a replacement token, which we store: for a user
   * forced here by `mustChangePassword` it is the token that lifts the restriction; for everyone else
   * it simply refreshes the session.
   */
  changePassword(
    currentPassword: string,
    newPassword: string,
    confirmNewPassword: string,
  ): Observable<ChangePasswordResponse> {
    return this.http
      .post<ChangePasswordResponse>(`${environment.apiUrl}/Auth/change-password`, {
        currentPassword,
        newPassword,
        confirmNewPassword,
      })
      .pipe(tap((response) => this.applyAccessToken(response.accessToken)));
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

  /** Swap in a replacement token (identity unchanged); ignored when the server sent none. */
  private applyAccessToken(accessToken: string | undefined): void {
    const current = this.profile();
    if (!current || !accessToken) return;
    this.setSession({ ...current, accessToken, mustChangePassword: false });
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
  const claim = decodeClaim(token, ROLE_CLAIM_URI) ?? decodeClaim(token, 'role') ?? decodeClaim(token, 'roles');
  if (Array.isArray(claim)) return claim.map((r) => String(r));
  if (typeof claim === 'string') return [claim];
  return [];
}

/**
 * True when the token carries the backend's `mustChangePassword` claim — i.e. it is a restricted token
 * that may only reach the change-password flow.
 */
export function decodeMustChangePassword(token: string | null): boolean {
  return decodeClaim(token, MUST_CHANGE_PASSWORD_CLAIM) === 'true';
}

/** Read one claim out of the JWT payload; null for a missing claim or an undecodable token. */
function decodeClaim(token: string | null, claim: string): unknown {
  if (!token) return null;
  const segments = token.split('.');
  if (segments.length < 2) return null;
  try {
    const payload = JSON.parse(base64UrlDecode(segments[1])) as Record<string, unknown>;
    return payload[claim] ?? null;
  } catch {
    return null;
  }
}

function base64UrlDecode(value: string): string {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  return atob(padded);
}
