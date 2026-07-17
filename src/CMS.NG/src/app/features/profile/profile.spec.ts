import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { environment } from '@environments/environment';
import { AuthService } from '@core/auth/auth.service';
import { UserProfile } from '@core/models/auth.model';
import { Profile } from './profile';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function makeToken(roles: string[]): string {
  const b64 = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64({ [ROLE_CLAIM]: roles })}.sig`;
}

function signIn(roles: string[]): void {
  const profile: UserProfile = {
    userId: 'helen',
    userName: 'Helen Wang',
    accessToken: makeToken(roles),
  };
  sessionStorage.setItem('cms.auth', JSON.stringify(profile));
}

describe('Profile', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    // Sign in BEFORE AuthService is instantiated so it reads the profile from session storage.
    signIn(['Admin', 'User']);

    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        MessageService,
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('shows the UserId as read-only (not an input) and the roles', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;

    const userId = el.querySelector('[data-testid="profile-user-id"]');
    expect(userId?.textContent).toContain('helen');
    // Read-only: rendered as text, not an editable control.
    expect(userId?.tagName).not.toBe('INPUT');

    const rolesText = el.querySelector('[data-testid="profile-roles"]')?.textContent ?? '';
    expect(rolesText).toContain('Admin');
    expect(rolesText).toContain('User');
  });

  it('pre-fills the editable UserName from the current profile', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    expect(fixture.componentInstance.form.controls.userName.value).toBe('Helen Wang');
  });

  it('saving updates the shell name and session storage', () => {
    const auth = TestBed.inject(AuthService);
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();

    fixture.componentInstance.form.controls.userName.setValue('Helen Renamed');
    fixture.componentInstance.save();

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/profile`);
    expect(req.request.method).toBe('PUT');
    // Only userName is sent — never a userId.
    expect(req.request.body).toEqual({ userName: 'Helen Renamed' });
    req.flush({ userId: 'helen', userName: 'Helen Renamed' });

    // The shell binds auth.userName(); it now reflects the new name.
    expect(auth.userName()).toBe('Helen Renamed');
    const stored = JSON.parse(sessionStorage.getItem('cms.auth')!) as UserProfile;
    expect(stored.userName).toBe('Helen Renamed');
    // UserId and token are untouched.
    expect(stored.userId).toBe('helen');
  });

  it('does not call the API when UserName is only whitespace', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();

    fixture.componentInstance.form.controls.userName.setValue('   ');
    fixture.componentInstance.save();

    httpMock.expectNone(`${environment.apiUrl}/Auth/profile`);
    expect(fixture.componentInstance.form.controls.userName.invalid).toBeTrue();
  });

  // ---- Change Password: client-side validation ----

  it('flags a weak new password (length < 8 or < 3 classes) as invalid', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    const pw = fixture.componentInstance.passwordForm;

    pw.controls.newPassword.setValue('Ab1!'); // 4 classes but too short
    expect(pw.controls.newPassword.hasError('complexity')).toBeTrue();

    pw.controls.newPassword.setValue('lowercase123'); // long enough but only 2 classes
    expect(pw.controls.newPassword.hasError('complexity')).toBeTrue();

    pw.controls.newPassword.setValue('Abcdef1!'); // strong
    expect(pw.controls.newPassword.hasError('complexity')).toBeFalse();
  });

  it('flags a new/confirm mismatch at the group level', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    const pw = fixture.componentInstance.passwordForm;

    pw.controls.currentPassword.setValue('secret123');
    pw.controls.newPassword.setValue('Abcdef1!');
    pw.controls.confirmNewPassword.setValue('Different1!');
    expect(pw.hasError('mismatch')).toBeTrue();
    expect(pw.invalid).toBeTrue();

    pw.controls.confirmNewPassword.setValue('Abcdef1!');
    expect(pw.hasError('mismatch')).toBeFalse();
    expect(pw.valid).toBeTrue();
  });

  it('does not call the API when the password form is invalid', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();

    // Weak new password + mismatch — must not reach the server.
    fixture.componentInstance.passwordForm.controls.currentPassword.setValue('secret123');
    fixture.componentInstance.passwordForm.controls.newPassword.setValue('weak');
    fixture.componentInstance.passwordForm.controls.confirmNewPassword.setValue('nope');
    fixture.componentInstance.changePassword();

    httpMock.expectNone(`${environment.apiUrl}/Auth/change-password`);
    expect(fixture.componentInstance.passwordForm.invalid).toBeTrue();
  });

  it('posts only plaintext fields when the password form is valid', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();

    fixture.componentInstance.passwordForm.controls.currentPassword.setValue('secret123');
    fixture.componentInstance.passwordForm.controls.newPassword.setValue('Abcdef1!');
    fixture.componentInstance.passwordForm.controls.confirmNewPassword.setValue('Abcdef1!');
    fixture.componentInstance.changePassword();

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/change-password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      currentPassword: 'secret123',
      newPassword: 'Abcdef1!',
      confirmNewPassword: 'Abcdef1!',
    });
    // No hash of any kind is ever sent.
    expect(JSON.stringify(req.request.body).toLowerCase()).not.toContain('hash');
    req.flush({ message: 'ok' });
  });

  it('surfaces a server rejection (e.g. wrong current password) inline', () => {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();

    fixture.componentInstance.passwordForm.controls.currentPassword.setValue('wrong');
    fixture.componentInstance.passwordForm.controls.newPassword.setValue('Abcdef1!');
    fixture.componentInstance.passwordForm.controls.confirmNewPassword.setValue('Abcdef1!');
    fixture.componentInstance.changePassword();

    const req = httpMock.expectOne(`${environment.apiUrl}/Auth/change-password`);
    req.flush({ message: '目前密碼不正確。Current password is incorrect.' }, {
      status: 400,
      statusText: 'Bad Request',
    });

    expect(fixture.componentInstance.passwordError()).toContain('Current password is incorrect');
  });
});
