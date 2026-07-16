import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { environment } from '@environments/environment';
import { authInterceptor, GLOBAL_TOAST_KEY } from './auth.interceptor';
import { UserProfile } from '@core/models/auth.model';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  let messageService: { add: jasmine.Spy };

  const seedToken = (token: string) => {
    const profile: UserProfile = { userId: 'helen', userName: 'Helen Wang', accessToken: token };
    sessionStorage.setItem('cms.auth', JSON.stringify(profile));
  };

  beforeEach(() => {
    sessionStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };
    messageService = { add: jasmine.createSpy('add') };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: MessageService, useValue: messageService },
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
    expect(messageService.add).not.toHaveBeenCalled();
  });

  it('on 500 surfaces a friendly error toast using the safe message from the response body', () => {
    seedToken('abc.def.ghi');

    http.get(`${environment.apiUrl}/app-roles`).subscribe({ next: () => {}, error: () => {} });

    const req = httpMock.expectOne(`${environment.apiUrl}/app-roles`);
    req.flush(
      { message: 'An unexpected error occurred.' },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(messageService.add).toHaveBeenCalledTimes(1);
    const arg = messageService.add.calls.mostRecent().args[0];
    expect(arg.key).toBe(GLOBAL_TOAST_KEY);
    expect(arg.severity).toBe('error');
    expect(arg.detail).toBe('An unexpected error occurred.');

    // A 500 must not clear the session or redirect.
    expect(sessionStorage.getItem('cms.auth')).not.toBeNull();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('on a 500 with no message body shows a generic fallback toast', () => {
    seedToken('abc.def.ghi');

    http.get(`${environment.apiUrl}/app-roles`).subscribe({ next: () => {}, error: () => {} });

    const req = httpMock.expectOne(`${environment.apiUrl}/app-roles`);
    req.flush('boom', { status: 503, statusText: 'Service Unavailable' });

    expect(messageService.add).toHaveBeenCalledTimes(1);
    const arg = messageService.add.calls.mostRecent().args[0];
    expect(arg.severity).toBe('error');
    expect(typeof arg.detail).toBe('string');
    expect(arg.detail.length).toBeGreaterThan(0);
  });
});
