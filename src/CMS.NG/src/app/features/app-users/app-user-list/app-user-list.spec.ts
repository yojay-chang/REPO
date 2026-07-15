import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserList } from './app-user-list';
import { AppUserService } from '@core/services/app-user.service';
import { AppUser, AppRoleLookup } from '@core/models/app-user.model';

const USERS: AppUser[] = [
  { pkid: 1, userId: 'helen', userName: 'Helen Wang', isActive: true, passwordUpdatedTime: null, roleCount: 2, roleIds: [] },
  { pkid: 2, userId: 'miles', userName: 'Miles Sun', isActive: false, passwordUpdatedTime: null, roleCount: 1, roleIds: [] },
];

const ROLES: AppRoleLookup[] = [
  { roleId: 'Admin', roleName: 'Administrator' },
  { roleId: 'User', roleName: 'User' },
];

describe('AppUserList', () => {
  let serviceSpy: jasmine.SpyObj<AppUserService>;

  function setup() {
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['query', 'delete', 'getAppRoles']);
    serviceSpy.query.and.returnValue(of(USERS));
    serviceSpy.delete.and.returnValue(of(void 0));
    serviceSpy.getAppRoles.and.returnValue(of(ROLES));

    TestBed.configureTestingModule({
      imports: [AppUserList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: AppUserService, useValue: serviceSpy },
      ],
    });

    return TestBed.createComponent(AppUserList);
  }

  beforeEach(() => sessionStorage.clear());

  it('loads users and role options on init', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(fixture.componentInstance.users().length).toBe(2);
    expect(fixture.componentInstance.roleOptions().length).toBe(2);
  });

  it('renders a row per user', () => {
    const fixture = setup();
    fixture.detectChanges();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilter persists filters and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = 'helen';
    cmp.applyFilter();

    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(cmp.filterVisible()).toBeFalse();
    expect(sessionStorage.getItem('app-user-list-filters')).toContain('helen');
  });

  it('clearFilter resets the filter and removes stored filters', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = 'helen';
    cmp.applyFilter();
    cmp.clearFilter();

    expect(cmp.filter.keyword).toBeNull();
    expect(cmp.filter.roleId).toBeNull();
    expect(sessionStorage.getItem('app-user-list-filters')).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.view(USERS[0]);
    expect(navSpy).toHaveBeenCalledWith(['/app-users', 'helen']);
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    fixture.componentInstance.confirmDelete(USERS[0]);

    expect(serviceSpy.delete).toHaveBeenCalledWith('helen');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error toast when loading fails', () => {
    const fixture = setup();
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    const messages = TestBed.inject(MessageService);
    const addSpy = spyOn(messages, 'add');

    fixture.componentInstance.load();
    expect(addSpy).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'error' }));
  });
});
