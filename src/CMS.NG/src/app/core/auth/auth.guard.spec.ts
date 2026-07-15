import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { UserProfile } from '@core/models/auth.model';

describe('authGuard', () => {
  let router: Router;

  const runGuard = () =>
    TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
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
    const profile: UserProfile = {
      userId: 'helen',
      userName: 'Helen Wang',
      accessToken: 'abc.def.ghi',
    };
    sessionStorage.setItem('cms.auth', JSON.stringify(profile));

    expect(runGuard()).toBeTrue();
  });
});
