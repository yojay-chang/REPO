# Auth (JWT, end-to-end)

Login + JWT bearer auth. Core rules + pointer index live in [`CLAUDE.md`](../CLAUDE.md); read this
when touching auth. The signing secret is the `symmetricSecurityKey` in `SysConfig['appConfig']`
(same JSON that holds `defaultPassword`) — read at runtime, never hard-coded. Config prerequisite:
that JSON **must** contain a `symmetricSecurityKey` ≥ 32 bytes (256 bits) or HS256 signing/validation throws.

## Backend (`src/CMS.API`)

- `POST /api/Auth/login` (`AuthController`, `[AllowAnonymous]`) →
  `{ userId, userName, accessToken, mustChangePassword }`. Checks `UserId` + `IsActive=1` +
  `PasswordHash == SHA256(password)`; any failure → generic 401 `invalid credentials`. `AuthCredential`
  (carries `PasswordHash`) is backend-only, never serialized. Also compares the stored hash to
  `IAuthRepository.GetDefaultPasswordHashAsync()` — see **Forced first-login password change** below.
- `PUT /api/Auth/profile` (`[Authorize]`, overrides the class `[AllowAnonymous]`) → `{ userId, userName }`.
  Self-service name edit: updates **only** `UserName` for the `userId` claim in the JWT — never from the
  body (`UpdateProfileRequest.UserId` is ignored), so UserId/roles are unchangeable. `UserName` is
  required + trimmed (400 otherwise). `IAuthRepository.UpdateUserNameAsync`. Tests: `ProfileEndpointTests`.
- `POST /api/Auth/change-password` (`[Authorize]`) — self-service password change for the `userId` claim
  (never the body). Order: (1) `CurrentPassword` must equal the stored `PasswordHash` (SHA-256) else 400;
  (2) `NewPassword` must pass `Auth/PasswordPolicy` (len ≥ 8 **and** ≥ 3 of upper/lower/digit/symbol) else
  400 with `PasswordPolicy.ComplexityMessage`; (3) `NewPassword == ConfirmNewPassword` else 400;
  (4) `NewPassword` must **not** be the system default else 400 — accepting it would leave the account in
  the must-change state and bounce the user straight back. On success
  `IAuthRepository.UpdatePasswordAsync` sets `PasswordHash = SHA256(new)` + stamps `PasswordUpdatedTime`,
  and the response is `{ message, accessToken }` — a **fresh token without** the `mustChangePassword`
  claim (the caller's old token keeps the restriction until replaced). No hash ever crosses the wire.
  Tests: `ChangePasswordEndpointTests` (fresh factory per test), `ForcedPasswordChangeTests`.
- `POST /api/Auth/reset-password` — **Admin-only** admin action: reset another user's password to the
  system default. Body is `ResetPasswordRequest { UserId }` (target only — never a password/hash). Because
  the class-level `[AllowAnonymous]` bypasses a declarative `[Authorize]` on the action, the action enforces
  auth + role **explicitly by the JWT role claim**: anonymous → 401, authenticated non-Admin → 403 (`Forbid()`),
  Admin → reset. `IAuthRepository.ResetPasswordToDefaultAsync` reads `SysConfig['appConfig'].defaultPassword`
  at runtime, sets `PasswordHash = SHA256(default)` + stamps `PasswordUpdatedTime`; returns 404 for a missing
  user. Tests: `ResetPasswordEndpointTests` (401/403/200, hash = SHA256(default), no hash leaked; fresh factory).
- `IAuthRepository`/`AuthRepository` — credential + roles lookup; `GetSigningKeyAsync()` reads the key.
- `Auth/JwtTokenGenerator` — HS256, 24h lifetime; claims: `userId`, `userName`, one `ClaimTypes.Role`
  per `AppUserRole.RoleId`. Clears the outbound claim-type map so role claims serialize verbatim.
- `Program.cs` — `AddJwtBearer` with an `IssuerSigningKeyResolver` that pulls the key from
  `IAuthRepository` per validation; global `FallbackPolicy = RequireAuthenticatedUser()` protects
  **every** controller; `AuthController` opts out via `[AllowAnonymous]`. `UseAuthentication()` before
  `UseAuthorization()`.
### Forced first-login password change

A user who has never signed in — or whose account an Admin just reset — is still on the system default
password. Both are the same risk, and both are caught by one check: at login,
`PasswordHash == SHA256(SysConfig['appConfig'].defaultPassword)` via `IAuthRepository.GetDefaultPasswordHashAsync()`
(the plaintext default never leaves the repository).

- Such a login **still gets a token**, but a restricted one: `JwtTokenGenerator.Generate(..., mustChangePassword: true)`
  adds the `mustChangePassword` claim (`JwtTokenGenerator.MustChangePasswordClaim`, value `"true"`), and
  `LoginResponse.MustChangePassword` mirrors it for the UI. The **claim** is what is enforced, not the flag.
- `Middleware/PasswordChangeRequiredMiddleware` does the enforcing, and is why this is not merely a UI rule:
  a token carrying the claim is refused on every `/api` path except `login`, `change-password`, and `profile`,
  with **403** `{ message, code: "password_change_required" }`. Registered **after** `UseAuthentication()`/
  `UseAuthorization()` so `context.User` is populated (an anonymous caller has already been 401'd by the
  fallback policy). Non-`/api` paths (Swagger, static files) are untouched.
- The restriction lifts only by changing the password: `change-password` returns a fresh token without the
  claim. Nothing server-side "remembers" the forced state — it is re-derived from the stored hash on every login.
- Tests: `ForcedPasswordChangeTests` — the login flag, 403 + code on another endpoint, change-password still
  reachable, a normal token unaffected, the fresh token unlocking the API, the default password refused as a
  new password, and an Admin reset re-arming the rule for the target's next login. `FakeAuthRepository` seeds
  **dana**, a user still on `DefaultPassword`.

- **Tests**: `FakeRepositoryApiFactory` (base, swaps repos to fakes). `AppRoleApiFactory` clears the
  fallback policy so existing controller tests run un-authenticated; `AuthorizationApiFactory` keeps
  real enforcement (`AuthorizationTests`: 401 without/with-bad token, 200 with token, Auth anonymous).

## Frontend (`src/CMS.NG`)

- `core/auth/auth.service.ts` — login/logout; stores `{userId,userName,accessToken}` in **session**
  storage (`cms.auth`); `roles`/`isAdmin` decoded from the token (`decodeRoles`), no extra API call.
  `mustChangePassword` is likewise decoded from the token claim (`decodeMustChangePassword`) — not from the
  login flag — so it survives a reload and clears the instant `changePassword` stores the fresh token.
- `core/auth/auth.interceptor.ts` — attaches `Authorization: Bearer <token>`; on 401 clears session +
  redirects to `/login`; on **403 with `code: "password_change_required"`** navigates to
  `/change-password` **without** clearing the session (the token is valid, just restricted). Registered via
  `withInterceptors` in `app.config.ts`.
- `core/auth/auth.guard.ts` — `authGuard`/`authChildGuard` guard the pathless protected parent in
  `app.routes.ts`; `/login` is the only public route. The guard also **pins** a user with
  `mustChangePassword()` to `CHANGE_PASSWORD_ROUTE` (`/change-password`) and, conversely, redirects anyone
  else off that page to `/` — it exists only for the forced flow.
- `features/change-password/*` — the forced first-login page (protected route, but rendered **outside** the
  nav shell like `/login`: `App.showShell()` is `isAuthenticated() && !mustChangePassword()`). Current/new/
  confirm validated client-side by `password-policy`; server rejections (e.g. reusing the default) shown
  inline. On success `AuthService` stores the returned token, which clears `mustChangePassword()` and lets
  the guard through to `/`. The only other way out is the page's own logout link. Spec: `change-password.spec`.
- `features/login/*` — the login page. `App` shell renders only when authenticated, shows the
  signed-in `userName` (a `.profile-link` to `/profile`) + a logout button, and hides the
  **系統管理 Admin** nav group unless `auth.isAdmin()` (roles include `Admin`).
- `core/auth/password-policy.ts` — client mirror of the backend `PasswordPolicy`: `isPasswordComplex`,
  `passwordComplexityValidator` / `passwordsMatchValidator` (group-level), and `PASSWORD_COMPLEXITY_MESSAGE`.
- `features/profile/*` — **My Profile** (`/profile`, protected). Shows UserId + roles read-only,
  UserName editable; `auth.updateProfile(userName)` PUTs `/Auth/profile` and, on success, refreshes
  `userName` in the profile signal + session storage (token/userId kept) so the shell updates live.
  A separate **Change Password** form (current/new/confirm) validated client-side by `password-policy`;
  `auth.changePassword(...)` POSTs `/Auth/change-password` (plaintext only) — server rejections (e.g. wrong
  current password / complexity) are shown inline. The session/token is unchanged on success.
- `features/app-users/app-user-form/*` — the AppUser edit form hosts an Admin-only **重設密碼 Reset
  Password** button (shown only in edit mode when `auth.isAdmin()`), which confirms then calls
  `AppUserService.resetPassword(userId)` → `POST /Auth/reset-password { userId }` (only the userId is sent;
  no password/hash). The button is UI convenience only — the backend independently enforces the Admin role.
- **Tests**: `auth.interceptor.spec` (attaches Bearer, omits header when no token, 401 clears session +
  redirects to `/login`), `auth.guard.spec` (redirect when no token vs allow when present),
  `auth.service.spec` (login stores profile in session, logout clears, `isAdmin`/roles decoded from the
  token, `updateProfile` PUTs only `userName` and refreshes the profile), `app.spec` (shell hidden when
  signed out; **系統管理 Admin** nav shown only when roles include `Admin`; user name links to `/profile`),
  `profile.spec` (UserId/roles read-only, saving updates the shell name + session storage; password-form
  client validation — weak/mismatch blocks the call, valid POSTs only plaintext), `password-policy.spec`.
