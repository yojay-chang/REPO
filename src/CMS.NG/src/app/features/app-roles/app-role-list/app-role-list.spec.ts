import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppRoleList } from './app-role-list';
import { AppRoleService } from '@core/services/app-role.service';
import { AppRole } from '@core/models/app-role.model';

const ROLES: AppRole[] = [
  { pkid: 1, roleId: 'Admin', roleName: 'Administrator', permissionLevel: 1, description: '系統管理員', userCount: 3, userIds: [] },
  { pkid: 2, roleId: 'User', roleName: 'User', permissionLevel: 100, description: '一般使用者', userCount: 9, userIds: [] },
];

describe('AppRoleList', () => {
  let serviceSpy: jasmine.SpyObj<AppRoleService>;

  function setup() {
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(ROLES));
    serviceSpy.delete.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [AppRoleList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: AppRoleService, useValue: serviceSpy },
      ],
    });

    const fixture = TestBed.createComponent(AppRoleList);
    return fixture;
  }

  beforeEach(() => sessionStorage.clear());

  it('loads roles on init', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(fixture.componentInstance.roles().length).toBe(2);
  });

  it('renders a row per role', () => {
    const fixture = setup();
    fixture.detectChanges();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilter persists filters and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = 'admin';
    cmp.applyFilter();

    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(cmp.filterVisible()).toBeFalse();
    expect(sessionStorage.getItem('app-role-list-filters')).toContain('admin');
  });

  it('clearFilter resets the filter and removes stored filters', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = 'admin';
    cmp.applyFilter();
    cmp.clearFilter();

    expect(cmp.filter.keyword).toBeNull();
    expect(sessionStorage.getItem('app-role-list-filters')).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.view(ROLES[0]);
    expect(navSpy).toHaveBeenCalledWith(['/app-roles', 'Admin']);
  });

  it('edit navigates to the edit route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.edit(ROLES[0]);
    expect(navSpy).toHaveBeenCalledWith(['/app-roles', 'Admin', 'edit']);
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    fixture.componentInstance.confirmDelete(ROLES[0]);

    expect(serviceSpy.delete).toHaveBeenCalledWith('Admin');
    // reload after delete => query called on init + after delete
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
