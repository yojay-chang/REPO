import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserForm } from './app-user-form';
import { AppUserService } from '@core/services/app-user.service';
import { AuthService } from '@core/auth/auth.service';
import { AppUser, AppRoleLookup } from '@core/models/app-user.model';

const USER: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Wang',
  isActive: true,
  passwordUpdatedTime: null,
  roleCount: 1,
  roleIds: ['Admin'],
};

const ROLES: AppRoleLookup[] = [
  { roleId: 'Admin', roleName: 'Administrator' },
  { roleId: 'User', roleName: 'User' },
];

describe('AppUserForm', () => {
  let serviceSpy: jasmine.SpyObj<AppUserService>;

  function setup(id: string | null, isAdmin = true) {
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', [
      'getById',
      'getAppRoles',
      'create',
      'update',
      'resetPassword',
    ]);
    serviceSpy.getById.and.returnValue(of(USER));
    serviceSpy.getAppRoles.and.returnValue(of(ROLES));
    serviceSpy.create.and.returnValue(of(USER));
    serviceSpy.update.and.returnValue(of(USER));
    serviceSpy.resetPassword.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [AppUserForm],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: AppUserService, useValue: serviceSpy },
        { provide: AuthService, useValue: { isAdmin: signal(isAdmin) } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(AppUserForm);
  }

  /** The reset-password button, located by its label text, or null if not rendered. */
  function resetButton(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button'));
    return (buttons.find((b) => b.textContent?.includes('重設密碼')) as HTMLElement) ?? null;
  }

  describe('add mode', () => {
    it('starts in create mode with isActive defaulting to true', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeFalse();
      expect(cmp.form.controls.userId.enabled).toBeTrue();
      expect(cmp.form.controls.isActive.value).toBeTrue();
      expect(serviceSpy.getById).not.toHaveBeenCalled();
    });

    it('does not save an invalid form', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      fixture.componentInstance.save();
      expect(serviceSpy.create).not.toHaveBeenCalled();
    });

    it('creates and navigates on a valid submit', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      const cmp = fixture.componentInstance;
      const router = TestBed.inject(Router);
      const navSpy = spyOn(router, 'navigate');

      cmp.form.patchValue({
        userId: 'jenny',
        userName: 'Jenny Tsao',
        isActive: true,
        roleIds: ['User'],
      });
      cmp.save();

      expect(serviceSpy.create).toHaveBeenCalledWith(
        jasmine.objectContaining({ userId: 'jenny', userName: 'Jenny Tsao', roleIds: ['User'] }),
      );
      expect(navSpy).toHaveBeenCalledWith(['/app-users', 'helen']);
    });
  });

  describe('edit mode', () => {
    it('loads the user, disables userId, and patches values', () => {
      const fixture = setup('helen');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeTrue();
      expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
      expect(cmp.form.controls.userId.disabled).toBeTrue();
      expect(cmp.form.controls.userName.value).toBe('Helen Wang');
      expect(cmp.form.getRawValue().userId).toBe('helen');
    });

    it('updates including the disabled userId via getRawValue', () => {
      const fixture = setup('helen');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      cmp.form.patchValue({ userName: 'Helen 2', roleIds: ['Admin', 'User'] });
      cmp.save();

      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({ userId: 'helen', userName: 'Helen 2', roleIds: ['Admin', 'User'] }),
      );
    });
  });

  describe('reset password (Admin only)', () => {
    it('shows the reset-password button in edit mode for an Admin', () => {
      const fixture = setup('helen', true);
      fixture.detectChanges();
      expect(resetButton(fixture)).not.toBeNull();
    });

    it('hides the reset-password button for a non-Admin', () => {
      const fixture = setup('helen', false);
      fixture.detectChanges();
      expect(resetButton(fixture)).toBeNull();
    });

    it('hides the reset-password button in add mode even for an Admin', () => {
      const fixture = setup(null, true);
      fixture.detectChanges();
      expect(resetButton(fixture)).toBeNull();
    });

    it('resets the password via the service when the confirmation is accepted', () => {
      const fixture = setup('helen', true);
      fixture.detectChanges();
      const confirmation = TestBed.inject(ConfirmationService);
      spyOn(confirmation, 'confirm').and.callFake((opts) => {
        opts.accept?.();
        return confirmation;
      });

      fixture.componentInstance.confirmResetPassword();

      expect(serviceSpy.resetPassword).toHaveBeenCalledWith('helen');
    });
  });

  it('builds role options as "name (id)"', () => {
    const fixture = setup(null);
    fixture.detectChanges();
    const options = fixture.componentInstance.roleOptions();
    expect(options).toContain(jasmine.objectContaining({ roleId: 'Admin', label: 'Administrator (Admin)' }));
  });
});
