/** Credentials posted to POST /api/Auth/login. */
export interface LoginRequest {
  userId: string;
  password: string;
}

/** Profile returned on a successful login and stored in session storage. */
export interface UserProfile {
  userId: string;
  userName: string;
  accessToken: string;

  /**
   * True when the account still uses the system default password and must change it before it can do
   * anything else. Informational only — `AuthService.mustChangePassword` reads the token claim, which is
   * what the backend actually enforces.
   */
  mustChangePassword?: boolean;
}

/** Response from POST /Auth/change-password — carries the replacement token issued on success. */
export interface ChangePasswordResponse {
  message?: string;
  /** A fresh token without the `mustChangePassword` claim; replaces the stored one. */
  accessToken?: string;
}

/** Response from PUT /Auth/profile — the authenticated user's id and updated name. */
export interface ProfileResponse {
  userId: string;
  userName: string;
}
