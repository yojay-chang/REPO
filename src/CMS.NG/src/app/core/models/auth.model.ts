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
}

/** Response from PUT /Auth/profile — the authenticated user's id and updated name. */
export interface ProfileResponse {
  userId: string;
  userName: string;
}
