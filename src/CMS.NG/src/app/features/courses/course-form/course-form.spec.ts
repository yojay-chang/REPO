import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { CourseForm } from './course-form';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';

const COURSE = {
  pkid: 2,
  title: 'CCNA 網路',
  officialTitle: null,
  courseId: 'CCNA-200',
  prodCourseId: 'PRD-CCNA',
  friendlyUrl: 'ccna-200',
  displayOrder: 2,
  partnerPkid: 2,
  courseGroupPkid: null,
  publishStatusPkid: 1,
  scheduleOn: '2026-02-01',
  scheduleOff: '2036-02-01',
  hour: 40,
  listPrice: 30000,
  learningCredit: 5,
  material: null,
  objective: null,
  target: null,
  prerequisites: null,
  outline: null,
  towardCertOrExam: null,
  note: null,
  otherInfo: null,
  canRepeat: false,
  partnerName: '思科',
  courseGroupDescription: null,
  publishStatusDescription: '草稿',
  certificationPkids: [],
  jobCategoryPkids: [2],
} as Course;

describe('CourseForm', () => {
  let serviceSpy: jasmine.SpyObj<CourseService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;

  function setup(id: string | null) {
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['getById', 'create', 'update']);
    serviceSpy.getById.and.returnValue(of(COURSE));
    serviceSpy.create.and.returnValue(of(COURSE));
    serviceSpy.update.and.returnValue(of(COURSE));

    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', [
      'getPartners',
      'getCourseGroups',
      'getPublishStatuses',
      'getCertifications',
      'getJobCategories',
    ]);
    lookupSpy.getPartners.and.returnValue(of([{ pkid: 2, name: '思科' }]));
    lookupSpy.getCourseGroups.and.returnValue(of([{ pkid: 3, description: '雲端服務' }]));
    lookupSpy.getPublishStatuses.and.returnValue(of([{ pkid: 1, description: '草稿' }]));
    lookupSpy.getCertifications.and.returnValue(of([{ pkid: 1, title: 'MCSA' }]));
    lookupSpy.getJobCategories.and.returnValue(of([{ pkid: 2, description: '網路工程師' }]));

    TestBed.configureTestingModule({
      imports: [CourseForm],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: CourseService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(CourseForm);
  }

  describe('add mode', () => {
    it('starts in create mode without loading a course', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      expect(fixture.componentInstance.isEdit()).toBeFalse();
      expect(serviceSpy.getById).not.toHaveBeenCalled();
    });

    it('does not save an invalid (empty) form', () => {
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
        title: 'Kubernetes',
        courseId: 'CKA-100',
        prodCourseId: 'PRD-CKA',
        friendlyUrl: 'cka-100',
        partnerPkid: 2,
        publishStatusPkid: 1,
        scheduleOn: new Date(2026, 2, 1),
        scheduleOff: new Date(2036, 2, 1),
      });
      cmp.save();

      expect(serviceSpy.create).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 0, title: 'Kubernetes', scheduleOn: '2026-03-01' }),
      );
      expect(navSpy).toHaveBeenCalledWith(['/courses', 2]);
    });
  });

  describe('edit mode', () => {
    it('loads the course and patches values', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeTrue();
      expect(serviceSpy.getById).toHaveBeenCalledWith(2);
      expect(cmp.pkidDisplay()).toBe(2);
      expect(cmp.form.controls.title.value).toBe('CCNA 網路');
      expect(cmp.form.controls.scheduleOn.value instanceof Date).toBeTrue();
    });

    it('updates with the loaded pkid on submit', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      cmp.form.patchValue({ title: 'CCNA 進階' });
      cmp.save();

      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 2, title: 'CCNA 進階' }),
      );
    });
  });

  describe('sticky action toolbar', () => {
    // Runs the same assertions for New (no id) and Edit (id '2').
    for (const [mode, id] of [
      ['New', null],
      ['Edit', '2'],
    ] as const) {
      it(`pins the Save/Cancel toolbar with sticky styling on the ${mode} form`, () => {
        const fixture = setup(id);
        fixture.detectChanges();

        const toolbar = (fixture.nativeElement as HTMLElement).querySelector(
          '.sticky-toolbar',
        ) as HTMLElement | null;

        expect(toolbar).withContext('toolbar element exists').not.toBeNull();
        expect(toolbar!.classList).toContain('sticky-toolbar');
        // The toolbar renders with sticky/pinned positioning.
        expect(getComputedStyle(toolbar!).position).toBe('sticky');

        // Save and Cancel remain present inside the pinned toolbar.
        const text = toolbar!.textContent ?? '';
        expect(text).toContain('儲存'); // Save
        expect(text).toContain('取消'); // Cancel
      });
    }
  });
});
