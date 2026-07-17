import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { environment } from '@environments/environment';
import { UserProfile } from '@core/models/auth.model';
import { AuthService } from '@core/auth/auth.service';
import { ChangePassword } from './change-password';

/** Build a token whose payload carries `claims` — only the payload segment is ever decoded. */
const tokenWith = (claims: Record<string, unknown>) =>
  `header.${btoa(JSON.stringify(claims))}.signature`;

const RESTRICTED_TOKEN = tokenWith({ userId: 'dana', mustChangePassword: 'true' });
const FRESH_TOKEN = tokenWith({ userId: 'dana' });

const DEFAULT_PASSWORD = 'Default-P@ss1';
const NEW_PASSWORD = 'Str0ng!Pass';

describe('ChangePassword', () => {
  let fixture: ComponentFixture<ChangePassword>;
  let component: ChangePassword;
  let httpMock: HttpTestingController;
  let auth: AuthService;
  let router: { navigate: jasmine.Spy };

  const url = `${environment.apiUrl}/Auth/change-password`;

  beforeEach(async () => {
    sessionStorage.clear();
    // Signed in with a restricted token — the state that forces this page.
    const profile: UserProfile = {
      userId: 'dana',
      userName: 'Dana Chen',
      accessToken: RESTRICTED_TOKEN,
    };
    sessionStorage.setItem('cms.auth', JSON.stringify(profile));

    router = { navigate: jasmine.createSpy('navigate') };

    await TestBed.configureTestingModule({
      imports: [ChangePassword],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        { provide: Router, useValue: router },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChangePassword);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  const fillForm = (current: string, next: string, confirm: string) =>
    component.form.setValue({
      currentPassword: current,
      newPassword: next,
      confirmNewPassword: confirm,
    });

  it('starts in the must-change state', () => {
    expect(auth.mustChangePassword()).toBeTrue();
  });

  it('does not call the API when the new password is too weak', () => {
    fillForm(DEFAULT_PASSWORD, 'weak', 'weak');

    component.submit();

    httpMock.expectNone(url);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not call the API when the confirmation does not match', () => {
    fillForm(DEFAULT_PASSWORD, NEW_PASSWORD, 'Different1!');

    component.submit();

    httpMock.expectNone(url);
  });

  it('sends only plaintext fields and, on success, stores the fresh token and leaves the page', () => {
    fillForm(DEFAULT_PASSWORD, NEW_PASSWORD, NEW_PASSWORD);

    component.submit();

    const req = httpMock.expectOne(url);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      currentPassword: DEFAULT_PASSWORD,
      newPassword: NEW_PASSWORD,
      confirmNewPassword: NEW_PASSWORD,
    });

    req.flush({ message: 'Password changed.', accessToken: FRESH_TOKEN });

    // The replacement token lifts the restriction and releases the rest of the app.
    expect(auth.mustChangePassword()).toBeFalse();
    expect(auth.token).toBe(FRESH_TOKEN);
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('shows the server message inline when the change is rejected, and stays put', () => {
    fillForm(DEFAULT_PASSWORD, NEW_PASSWORD, NEW_PASSWORD);

    component.submit();

    httpMock
      .expectOne(url)
      .flush(
        { message: 'The new password must not be the system default.' },
        { status: 400, statusText: 'Bad Request' },
      );

    expect(component.errorMessage()).toBe('The new password must not be the system default.');
    expect(router.navigate).not.toHaveBeenCalled();
    expect(auth.mustChangePassword()).toBeTrue();
  });

  it('logout clears the session and returns to /login', () => {
    component.logout();

    expect(sessionStorage.getItem('cms.auth')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
