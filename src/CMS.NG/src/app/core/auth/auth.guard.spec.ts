import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { UserProfile } from '@core/models/auth.model';

/** Build a token whose payload carries `claims` — only the payload segment is ever decoded. */
const tokenWith = (claims: Record<string, unknown>) =>
  `header.${btoa(JSON.stringify(claims))}.signature`;

/** Token of a user whose account still uses the system default password. */
const MUST_CHANGE_TOKEN = tokenWith({ mustChangePassword: 'true' });

const seedProfile = (accessToken: string) => {
  const profile: UserProfile = { userId: 'helen', userName: 'Helen Wang', accessToken };
  sessionStorage.setItem('cms.auth', JSON.stringify(profile));
};

describe('authGuard', () => {
  let router: Router;

  const runGuard = (url = '/app-roles') =>
    TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideRouter([])],
    });
    router = TestBed.inject(Router);
  });

  afterEach(() => sessionStorage.clear());

  it('redirects to /login (UrlTree) when there is no token', () => {
    const result = runGuard();
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login');
  });

  it('allows activation when a token is present in session storage', () => {
    seedProfile('abc.def.ghi');

    expect(runGuard()).toBeTrue();
  });

  describe('forced password change', () => {
    it('redirects to /change-password when the token says the default password is still in use', () => {
      seedProfile(MUST_CHANGE_TOKEN);

      const result = runGuard('/app-roles');

      expect(result).toBeInstanceOf(UrlTree);
      expect((result as UrlTree).toString()).toBe('/change-password');
    });

    it('lets that user open the change-password page itself (no redirect loop)', () => {
      seedProfile(MUST_CHANGE_TOKEN);

      expect(runGuard('/change-password')).toBeTrue();
    });

    it('keeps a normal user off the change-password page', () => {
      seedProfile('abc.def.ghi');

      const result = runGuard('/change-password');

      expect(result).toBeInstanceOf(UrlTree);
      expect((result as UrlTree).toString()).toBe('/');
    });

    it('still redirects to /login when signed out, even for /change-password', () => {
      const result = runGuard('/change-password');

      expect(result).toBeInstanceOf(UrlTree);
      expect((result as UrlTree).toString()).toBe('/login');
    });
  });
});
