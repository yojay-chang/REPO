import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { AuthService, decodeRoles } from './auth.service';
import { UserProfile } from '@core/models/auth.model';

/** Build an unsigned JWT with the given payload (only the payload segment matters for decoding). */
function makeToken(payload: Record<string, unknown>): string {
  const b64 = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64(payload)}.sig`;
}

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('login POSTs credentials and stores the profile in session storage', () => {
    const profile: UserProfile = {
      userId: 'helen',
      userName: 'Helen Wang',
      accessToken: makeToken({ [ROLE_CLAIM]: ['Admin', 'User'] }),
    };

    let received: UserProfile | undefined;
    service.login({ userId: 'helen', password: 'secret123' }).subscribe((p) => (received = p));

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'helen', password: 'secret123' });
    req.flush(profile);

    expect(received).toEqual(profile);
    expect(sessionStorage.getItem('cms.auth')).toBe(JSON.stringify(profile));
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.userName()).toBe('Helen Wang');
    expect(service.token).toBe(profile.accessToken);
  });

  it('logout clears session storage and the profile', () => {
    const profile: UserProfile = {
      userId: 'helen',
      userName: 'Helen Wang',
      accessToken: makeToken({}),
    };
    service.login({ userId: 'helen', password: 'x' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/Auth/login`).flush(profile);
    expect(service.isAuthenticated()).toBeTrue();

    service.logout();
    expect(sessionStorage.getItem('cms.auth')).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.token).toBeNull();
  });

  it('updateProfile PUTs only the userName and refreshes the stored profile (token + userId kept)', () => {
    const token = makeToken({ [ROLE_CLAIM]: ['Admin', 'User'] });
    service.login({ userId: 'helen', password: 'x' }).subscribe();
    httpMock
      .expectOne(`${environment.apiUrl}/Auth/login`)
      .flush({ userId: 'helen', userName: 'Helen Wang', accessToken: token });

    service.updateProfile('Helen Renamed').subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/profile`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ userName: 'Helen Renamed' });
    req.flush({ userId: 'helen', userName: 'Helen Renamed' });

    // Signal + session storage now reflect the new name; token and userId are unchanged.
    expect(service.userName()).toBe('Helen Renamed');
    const stored = JSON.parse(sessionStorage.getItem('cms.auth')!) as UserProfile;
    expect(stored).toEqual({ userId: 'helen', userName: 'Helen Renamed', accessToken: token });
  });

  it('exposes isAdmin=true when the token roles include Admin', () => {
    service.login({ userId: 'helen', password: 'x' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/Auth/login`).flush({
      userId: 'helen',
      userName: 'Helen Wang',
      accessToken: makeToken({ [ROLE_CLAIM]: ['Admin', 'User'] }),
    });
    expect(service.isAdmin()).toBeTrue();
  });

  it('exposes isAdmin=false when the token roles exclude Admin', () => {
    service.login({ userId: 'miles', password: 'x' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/Auth/login`).flush({
      userId: 'miles',
      userName: 'Miles Sun',
      accessToken: makeToken({ [ROLE_CLAIM]: ['User'] }),
    });
    expect(service.isAdmin()).toBeFalse();
  });
});

describe('decodeRoles', () => {
  it('reads the ClaimTypes.Role URI claim (array)', () => {
    const token = makeToken({ [ROLE_CLAIM]: ['Admin', 'User'] });
    expect(decodeRoles(token)).toEqual(['Admin', 'User']);
  });

  it('reads a single string role claim', () => {
    expect(decodeRoles(makeToken({ role: 'Admin' }))).toEqual(['Admin']);
  });

  it('returns [] for a null or malformed token', () => {
    expect(decodeRoles(null)).toEqual([]);
    expect(decodeRoles('garbage')).toEqual([]);
  });
});
