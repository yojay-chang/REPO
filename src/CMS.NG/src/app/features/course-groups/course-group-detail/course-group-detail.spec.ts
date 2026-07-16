import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { CourseGroupDetail } from './course-group-detail';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const COURSE_GROUP: CourseGroup = {
  pkid: 2,
  description: '網路管理',
};

describe('CourseGroupDetail', () => {
  let serviceSpy: jasmine.SpyObj<CourseGroupService>;

  function setup(id = '2') {
    serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', ['getById']);
    serviceSpy.getById.and.returnValue(of(COURSE_GROUP));

    TestBed.configureTestingModule({
      imports: [CourseGroupDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: CourseGroupService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(CourseGroupDetail);
  }

  it('loads the course group by numeric pkid', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(cmp.courseGroup()?.description).toBe('網路管理');
  });

  it('renders the course group fields in the template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('網路管理');
  });

  it('viewCourses navigates to the courses list filtered by course group', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.viewCourses();
    expect(navSpy).toHaveBeenCalledWith(['/courses'], { queryParams: { courseGroupPkid: 2 } });
  });
});
