import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { AppRoleDetail } from './app-role-detail';
import { AppRoleService } from '@core/services/app-role.service';
import { AppRole, AppUserLookup } from '@core/models/app-role.model';

const ROLE: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 2,
  userIds: ['helen', 'miles'],
};

const USERS: AppUserLookup[] = [
  { userId: 'helen', userName: 'helen' },
  { userId: 'miles', userName: 'Miles Sun' },
];

describe('AppRoleDetail', () => {
  let serviceSpy: jasmine.SpyObj<AppRoleService>;

  function setup(id = 'Admin') {
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['getById', 'getAppUsers']);
    serviceSpy.getById.and.returnValue(of(ROLE));
    serviceSpy.getAppUsers.and.returnValue(of(USERS));

    TestBed.configureTestingModule({
      imports: [AppRoleDetail],
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

    return TestBed.createComponent(AppRoleDetail);
  }

  it('loads the role and resolves user labels', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(serviceSpy.getById).toHaveBeenCalledWith('Admin');
    expect(cmp.role()?.roleName).toBe('Administrator');
    expect(cmp.userLabels()).toEqual(['helen (helen)', 'Miles Sun (miles)']);
  });

  it('renders the role fields in the template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Administrator');
    expect(text).toContain('系統管理員');
  });
});
