import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '@environments/environment';
import { authInterceptor } from './auth.interceptor';
import { UserProfile } from '@core/models/auth.model';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: { navigate: jasmine.Spy };

  const seedToken = (token: string) => {
    const profile: UserProfile = { userId: 'helen', userName: 'Helen Wang', accessToken: token };
    sessionStorage.setItem('cms.auth', JSON.stringify(profile));
  };

  beforeEach(() => {
    sessionStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('attaches the Bearer token from session storage to outgoing requests', () => {
    seedToken('abc.def.ghi');

    http.get(`${environment.apiUrl}/app-users`).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/app-users`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    req.flush([]);
  });

  it('does not attach an Authorization header when there is no token', () => {
    http.get(`${environment.apiUrl}/app-users`).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/app-users`);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('on 401 clears session storage and redirects to /login', () => {
    seedToken('abc.def.ghi');

    http.get(`${environment.apiUrl}/app-users`).subscribe({ next: () => {}, error: () => {} });

    const req = httpMock.expectOne(`${environment.apiUrl}/app-users`);
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem('cms.auth')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
