import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CourseGroupList } from './course-group-list';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const COURSE_GROUPS: CourseGroup[] = [
  { pkid: 1, description: '資料庫' },
  { pkid: 2, description: '網路管理' },
];

// p-table sorts its bound [value] array in place (default sort here is pkid DESC),
// so index into a stable copy rather than the rendered array.
const TARGET: CourseGroup = { pkid: 1, description: '資料庫' };

describe('CourseGroupList', () => {
  let serviceSpy: jasmine.SpyObj<CourseGroupService>;

  function setup() {
    serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(COURSE_GROUPS));
    serviceSpy.delete.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [CourseGroupList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: CourseGroupService, useValue: serviceSpy },
      ],
    });

    return TestBed.createComponent(CourseGroupList);
  }

  beforeEach(() => sessionStorage.clear());

  it('loads course groups on init', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(fixture.componentInstance.courseGroups().length).toBe(2);
  });

  it('renders a row per course group', () => {
    const fixture = setup();
    fixture.detectChanges();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilter persists filters and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = '資料庫';
    cmp.applyFilter();

    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(cmp.filterVisible()).toBeFalse();
    expect(sessionStorage.getItem('course-group-list-filters')).toContain('資料庫');
  });

  it('clearFilter resets the filter and removes stored filters', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = '資料庫';
    cmp.applyFilter();
    cmp.clearFilter();

    expect(cmp.filter.keyword).toBeNull();
    expect(sessionStorage.getItem('course-group-list-filters')).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.view(TARGET);
    expect(navSpy).toHaveBeenCalledWith(['/course-groups', 1]);
  });

  it('edit navigates to the edit route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.edit(TARGET);
    expect(navSpy).toHaveBeenCalledWith(['/course-groups', 1, 'edit']);
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    fixture.componentInstance.confirmDelete(TARGET);

    expect(serviceSpy.delete).toHaveBeenCalledWith(1);
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
