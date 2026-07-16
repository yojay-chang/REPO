import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { App } from './app';
import { UserProfile } from '@core/models/auth.model';

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

describe('App', () => {
  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), MessageService],
    }).compileComponents();
  });

  afterEach(() => sessionStorage.clear());

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('hides the shell (sidebar) when not signed in', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.layout-sidebar')).toBeNull();
  });

  it('renders the shell and the signed-in user name when authenticated', () => {
    signIn(['Admin']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.logo-text')?.textContent).toContain('UWA');
    expect(compiled.querySelector('.app-header-user')?.textContent).toContain('Helen Wang');
  });

  it('links the signed-in user name to the My Profile page', () => {
    signIn(['User']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const link = (fixture.nativeElement as HTMLElement).querySelector(
      '.app-header-user a.profile-link',
    );
    expect(link).not.toBeNull();
    expect(link?.getAttribute('href')).toContain('/profile');
    expect(link?.textContent).toContain('Helen Wang');
  });

  it('shows the 系統管理 Admin nav group for users whose roles include Admin', () => {
    signIn(['Admin', 'User']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('系統管理 Admin');
    expect(text).toContain('角色 AppRole');
  });

  it('hides the 系統管理 Admin nav group for users without the Admin role', () => {
    signIn(['User']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const text = compiled.textContent ?? '';
    expect(text).not.toContain('系統管理 Admin');
    // A non-admin still sees other groups.
    expect(text).toContain('課程管理 Course');
  });
});
