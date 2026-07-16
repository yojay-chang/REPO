import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { AppUserDetail } from './app-user-detail';
import { AppUserService } from '@core/services/app-user.service';
import { AppUser, AppRoleLookup } from '@core/models/app-user.model';

const USER: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Wang',
  isActive: true,
  passwordUpdatedTime: '2026-01-01T09:00:00',
  roleCount: 2,
  roleIds: ['Admin', 'User'],
};

const ROLES: AppRoleLookup[] = [
  { roleId: 'Admin', roleName: 'Administrator' },
  { roleId: 'User', roleName: 'User' },
];

describe('AppUserDetail', () => {
  let serviceSpy: jasmine.SpyObj<AppUserService>;

  function setup(id = 'helen') {
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', [
      'getById',
      'getAppRoles',
    ]);
    serviceSpy.getById.and.returnValue(of(USER));
    serviceSpy.getAppRoles.and.returnValue(of(ROLES));

    TestBed.configureTestingModule({
      imports: [AppUserDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: AppUserService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(AppUserDetail);
  }

  it('loads the user and resolves role labels', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
    expect(cmp.user()?.userName).toBe('Helen Wang');
    expect(cmp.roleLabels()).toEqual(['Administrator (Admin)', 'User (User)']);
  });

  it('renders the user fields in the template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Helen Wang');
    expect(text).toContain('Administrator (Admin)');
  });

  it('does not render a reset-password button (reset lives on the edit form)', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('重設密碼');
  });
});
