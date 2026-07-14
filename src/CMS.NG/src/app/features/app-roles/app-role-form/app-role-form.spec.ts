import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { AppRoleForm } from './app-role-form';
import { AppRoleService } from '@core/services/app-role.service';
import { AppRole, AppUserLookup } from '@core/models/app-role.model';

const ROLE: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 1,
  userIds: ['helen'],
};

const USERS: AppUserLookup[] = [
  { userId: 'helen', userName: 'helen' },
  { userId: 'miles', userName: 'Miles Sun' },
];

describe('AppRoleForm', () => {
  let serviceSpy: jasmine.SpyObj<AppRoleService>;

  function setup(id: string | null) {
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', [
      'getById',
      'getAppUsers',
      'create',
      'update',
    ]);
    serviceSpy.getById.and.returnValue(of(ROLE));
    serviceSpy.getAppUsers.and.returnValue(of(USERS));
    serviceSpy.create.and.returnValue(of(ROLE));
    serviceSpy.update.and.returnValue(of(ROLE));

    TestBed.configureTestingModule({
      imports: [AppRoleForm],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: AppRoleService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(AppRoleForm);
  }

  describe('add mode', () => {
    it('starts in create mode with a default permission level', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeFalse();
      expect(cmp.form.controls.roleId.enabled).toBeTrue();
      expect(cmp.form.controls.permissionLevel.value).toBe(100);
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
        roleId: 'Editor',
        roleName: 'Content Editor',
        permissionLevel: 50,
        description: '內容編輯',
        userIds: ['helen'],
      });
      cmp.save();

      expect(serviceSpy.create).toHaveBeenCalledWith(
        jasmine.objectContaining({ roleId: 'Editor', roleName: 'Content Editor', userIds: ['helen'] }),
      );
      expect(navSpy).toHaveBeenCalledWith(['/app-roles', 'Admin']);
    });
  });

  describe('edit mode', () => {
    it('loads the role, disables roleId, and patches values', () => {
      const fixture = setup('Admin');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeTrue();
      expect(serviceSpy.getById).toHaveBeenCalledWith('Admin');
      expect(cmp.form.controls.roleId.disabled).toBeTrue();
      expect(cmp.form.controls.roleName.value).toBe('Administrator');
      expect(cmp.form.getRawValue().roleId).toBe('Admin');
    });

    it('updates including the disabled roleId via getRawValue', () => {
      const fixture = setup('Admin');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      cmp.form.patchValue({ roleName: 'Administrator 2', userIds: ['helen', 'miles'] });
      cmp.save();

      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({ roleId: 'Admin', roleName: 'Administrator 2', userIds: ['helen', 'miles'] }),
      );
    });
  });

  it('builds user options as "name (id)"', () => {
    const fixture = setup(null);
    fixture.detectChanges();
    const options = fixture.componentInstance.userOptions();
    expect(options).toContain(jasmine.objectContaining({ userId: 'miles', label: 'Miles Sun (miles)' }));
  });
});
