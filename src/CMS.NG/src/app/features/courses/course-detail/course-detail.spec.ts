import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { CourseDetail } from './course-detail';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';

const COURSE = {
  pkid: 1,
  title: 'Azure 基礎',
  courseId: 'AZ-900',
  partnerPkid: 1,
  partnerName: '微軟',
  courseGroupPkid: 3,
  publishStatusPkid: 2,
  certificationPkids: [1],
  jobCategoryPkids: [2],
} as Course;

describe('CourseDetail', () => {
  let serviceSpy: jasmine.SpyObj<CourseService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;

  function setup(id = '1') {
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['getById']);
    serviceSpy.getById.and.returnValue(of(COURSE));

    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', [
      'getCertifications',
      'getJobCategories',
    ]);
    lookupSpy.getCertifications.and.returnValue(of([{ pkid: 1, title: 'MCSA' }]));
    lookupSpy.getJobCategories.and.returnValue(of([{ pkid: 2, description: '網路工程師' }]));

    TestBed.configureTestingModule({
      imports: [CourseDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
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

    return TestBed.createComponent(CourseDetail);
  }

  it('loads the course by numeric pkid and resolves N-N labels', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(serviceSpy.getById).toHaveBeenCalledWith(1);
    expect(cmp.course()?.title).toBe('Azure 基礎');
    expect(cmp.certificationLabels()).toEqual(['MCSA']);
    expect(cmp.jobCategoryLabels()).toEqual(['網路工程師']);
  });

  it('renders the course fields in the template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Azure 基礎');
    expect(text).toContain('微軟');
  });

  it('builds the QR-code URL from the course pkid and courseId', () => {
    const fixture = setup();
    fixture.detectChanges();

    expect(fixture.componentInstance.qrUrl()).toBe(
      'https://www.uuu.com.tw/Course/Show/1/AZ-900',
    );
  });

  it('shows the courseId as the QR-code title inside 基本資料', () => {
    const fixture = setup();
    fixture.detectChanges();

    const title = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qr-title"]');
    expect(title?.textContent?.trim()).toBe('AZ-900');
  });

  it('viewPartner navigates to the partner detail page', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.viewPartner();
    expect(navSpy).toHaveBeenCalledWith(['/partners', 1]);
  });

  it('viewFaqs navigates to the FAQ list filtered by course', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.viewFaqs();
    expect(navSpy).toHaveBeenCalledWith(['/course-faqs'], { queryParams: { coursePkid: 1 } });
  });
});
