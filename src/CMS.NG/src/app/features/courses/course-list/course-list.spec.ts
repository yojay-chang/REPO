import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CourseList } from './course-list';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';

const COURSES = [
  { pkid: 1, title: 'Azure 基礎', courseId: 'AZ-900', displayOrder: 1, partnerName: '微軟', canRepeat: true },
  { pkid: 2, title: 'CCNA 網路', courseId: 'CCNA-200', displayOrder: 2, partnerName: '思科', canRepeat: false },
] as Course[];

// A fully-populated list row for inline-edit tests (dates present for range validation).
const EDITABLE_COURSE = {
  pkid: 1,
  title: 'Azure 基礎',
  officialTitle: null,
  courseId: 'AZ-900',
  prodCourseId: 'PRD-AZ900',
  friendlyUrl: 'azure-900',
  displayOrder: 1,
  partnerPkid: 1,
  courseGroupPkid: 3,
  publishStatusPkid: 2,
  scheduleOn: '2026-01-01',
  scheduleOff: '2036-01-01',
  hour: 14,
  listPrice: 12000,
  learningCredit: 3,
  material: null,
  objective: null,
  target: null,
  prerequisites: null,
  outline: null,
  towardCertOrExam: null,
  note: null,
  otherInfo: null,
  canRepeat: true,
  partnerName: '微軟',
  courseGroupDescription: '雲端服務',
  publishStatusDescription: '已發布',
  certificationPkids: [],
  jobCategoryPkids: [],
} as Course;

// What getById returns — the full record carries the N-N lists the list row omits.
const FULL_COURSE = { ...EDITABLE_COURSE, certificationPkids: [1], jobCategoryPkids: [2] } as Course;

describe('CourseList', () => {
  let serviceSpy: jasmine.SpyObj<CourseService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;

  function setup(queryParams: Record<string, string> = {}) {
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', [
      'query',
      'delete',
      'getById',
      'update',
    ]);
    serviceSpy.query.and.returnValue(of(COURSES));
    serviceSpy.delete.and.returnValue(of(void 0));
    serviceSpy.getById.and.returnValue(of(FULL_COURSE));
    serviceSpy.update.and.returnValue(of(FULL_COURSE));

    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', [
      'getPartners',
      'getCourseGroups',
      'getPublishStatuses',
    ]);
    lookupSpy.getPartners.and.returnValue(of([{ pkid: 1, name: '微軟' }]));
    lookupSpy.getCourseGroups.and.returnValue(of([{ pkid: 3, description: '雲端服務' }]));
    lookupSpy.getPublishStatuses.and.returnValue(of([{ pkid: 2, description: '已發布' }]));

    TestBed.configureTestingModule({
      imports: [CourseList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: CourseService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: (k: string) => queryParams[k] ?? null } } },
        },
      ],
    });

    return TestBed.createComponent(CourseList);
  }

  beforeEach(() => sessionStorage.clear());

  it('loads lookups then courses on init', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(lookupSpy.getPartners).toHaveBeenCalled();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(fixture.componentInstance.courses().length).toBe(2);
    expect(fixture.componentInstance.partnerOptions().length).toBe(1);
  });

  it('renders a row per course', () => {
    const fixture = setup();
    fixture.detectChanges();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('pre-filters by incoming partnerPkid query param', () => {
    const fixture = setup({ partnerPkid: '2' });
    fixture.detectChanges();
    expect(fixture.componentInstance.filter.partnerPkid).toBe(2);
  });

  it('applyFilter persists filters and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = 'Azure';
    cmp.applyFilter();

    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(cmp.filterVisible()).toBeFalse();
    expect(sessionStorage.getItem('course-list-filters')).toContain('Azure');
  });

  it('clearFilter resets the filter and removes stored filters', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = 'Azure';
    cmp.applyFilter();
    cmp.clearFilter();

    expect(cmp.filter.keyword).toBeNull();
    expect(sessionStorage.getItem('course-list-filters')).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.view(COURSES[0]);
    expect(navSpy).toHaveBeenCalledWith(['/courses', 1]);
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    fixture.componentInstance.confirmDelete(COURSES[0]);

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

  describe('inline cell editing', () => {
    /** Render the list, then swap in a single fully-populated row for editing. */
    function setupWithRow() {
      const fixture = setup();
      fixture.detectChanges();
      const cmp = fixture.componentInstance;
      cmp.courses.set([{ ...EDITABLE_COURSE }]);
      fixture.detectChanges();
      return { fixture, cmp };
    }

    function cell(fixture: ReturnType<typeof setup>, field: string): HTMLElement {
      return (fixture.nativeElement as HTMLElement).querySelector(`td[data-field="${field}"]`)!;
    }

    it('enters edit mode on double-click, but a single click does not', () => {
      const { fixture, cmp } = setupWithRow();
      const titleCell = cell(fixture, 'title');

      // Single click must NOT start editing.
      titleCell.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();
      expect(cmp.editing()).toBeNull();
      expect(titleCell.querySelector('input')).toBeNull();

      // Double click starts editing and renders a text editor.
      titleCell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
      fixture.detectChanges();
      expect(cmp.editing()).toEqual({ pkid: 1, field: 'title' });
      expect(titleCell.querySelector('input')).not.toBeNull();
    });

    it('does not allow editing the three read-only columns', () => {
      const { fixture, cmp } = setupWithRow();

      for (const field of ['pkid', 'partnerName', 'courseGroupDescription']) {
        expect(cmp.isReadOnly(field)).toBeTrue();

        // No dblclick handler on read-only cells → editing stays closed.
        cell(fixture, field).dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
        fixture.detectChanges();
        expect(cmp.editing()).toBeNull();

        // Even a direct startEdit call is ignored for read-only columns.
        cmp.startEdit(EDITABLE_COURSE, field);
        expect(cmp.editing()).toBeNull();
      }
    });

    it('persists a valid edit on blur via the update endpoint, preserving the N-N lists', () => {
      const { cmp } = setupWithRow();

      cmp.startEdit(EDITABLE_COURSE, 'title');
      cmp.editValue = 'Azure 進階';
      cmp.commitEdit(EDITABLE_COURSE, 'title'); // blur

      expect(serviceSpy.getById).toHaveBeenCalledWith(1);
      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({
          pkid: 1,
          title: 'Azure 進階',
          certificationPkids: [1],
          jobCategoryPkids: [2],
        }),
      );
      expect(cmp.editing()).toBeNull(); // editor closes on success
    });

    it('persists a valid non-negative number edit', () => {
      const { cmp } = setupWithRow();

      cmp.startEdit(EDITABLE_COURSE, 'hour');
      cmp.editValue = 21;
      cmp.commitEdit(EDITABLE_COURSE, 'hour');

      expect(serviceSpy.update).toHaveBeenCalledWith(jasmine.objectContaining({ hour: 21 }));
    });

    it('blocks clearing a required field and keeps the cell in edit mode', () => {
      const { cmp } = setupWithRow();

      cmp.startEdit(EDITABLE_COURSE, 'title');
      cmp.editValue = '   ';
      cmp.commitEdit(EDITABLE_COURSE, 'title');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(cmp.editError()).not.toBeNull();
      expect(cmp.editing()).toEqual({ pkid: 1, field: 'title' }); // still editing
    });

    it('blocks a negative number in 時數 / 定價 / 點數', () => {
      const { cmp } = setupWithRow();

      for (const field of ['hour', 'listPrice', 'learningCredit'] as const) {
        cmp.startEdit(EDITABLE_COURSE, field);
        cmp.editValue = -1;
        cmp.commitEdit(EDITABLE_COURSE, field);

        expect(serviceSpy.update).not.toHaveBeenCalled();
        expect(cmp.editError()).not.toBeNull();
        cmp.cancelEdit();
      }
    });

    it('blocks a non-numeric / empty value in a numeric field', () => {
      const { cmp } = setupWithRow();

      cmp.startEdit(EDITABLE_COURSE, 'listPrice');
      cmp.editValue = null; // p-inputnumber emits null when cleared
      cmp.commitEdit(EDITABLE_COURSE, 'listPrice');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(cmp.editError()).not.toBeNull();
    });

    it('blocks an invalid date', () => {
      const { cmp } = setupWithRow();

      cmp.startEdit(EDITABLE_COURSE, 'scheduleOn');
      cmp.editValue = 'not-a-date';
      cmp.commitEdit(EDITABLE_COURSE, 'scheduleOn');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(cmp.editError()).not.toBeNull();
    });

    it('blocks 上架日期 later than 下架日期', () => {
      const { cmp } = setupWithRow();

      // scheduleOff is 2036-01-01 → a 2037 上架日期 is invalid.
      cmp.startEdit(EDITABLE_COURSE, 'scheduleOn');
      cmp.editValue = new Date(2037, 0, 1);
      cmp.commitEdit(EDITABLE_COURSE, 'scheduleOn');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(cmp.editError()).toContain('上架日期');
    });

    it('blocks 下架日期 earlier than 上架日期', () => {
      const { cmp } = setupWithRow();

      // scheduleOn is 2026-01-01 → a 2020 下架日期 is invalid.
      cmp.startEdit(EDITABLE_COURSE, 'scheduleOff');
      cmp.editValue = new Date(2020, 0, 1);
      cmp.commitEdit(EDITABLE_COURSE, 'scheduleOff');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(cmp.editError()).not.toBeNull();
    });

    it('accepts equal 上架日期 and 下架日期 (boundary)', () => {
      const { cmp } = setupWithRow();

      cmp.startEdit(EDITABLE_COURSE, 'scheduleOn');
      cmp.editValue = new Date(2036, 0, 1); // equals scheduleOff
      cmp.commitEdit(EDITABLE_COURSE, 'scheduleOn');

      expect(serviceSpy.update).toHaveBeenCalled();
    });

    it('reverts to the previous value and surfaces an error when the save fails', () => {
      const { cmp } = setupWithRow();
      serviceSpy.update.and.returnValue(throwError(() => new Error('boom')));
      const addSpy = spyOn(TestBed.inject(MessageService), 'add');

      cmp.startEdit(EDITABLE_COURSE, 'title');
      cmp.editValue = 'Azure 進階';
      cmp.commitEdit(EDITABLE_COURSE, 'title');

      // Row model was never mutated → still the original value (reverted), editor closed.
      expect(cmp.courses()[0].title).toBe('Azure 基礎');
      expect(cmp.editing()).toBeNull();
      expect(addSpy).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'error' }));
    });
  });
});
