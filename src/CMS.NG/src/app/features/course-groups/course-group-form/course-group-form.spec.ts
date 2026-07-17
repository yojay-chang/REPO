import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { CourseGroupForm } from './course-group-form';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const COURSE_GROUP: CourseGroup = {
  pkid: 2,
  description: '網路管理',
};

describe('CourseGroupForm', () => {
  let serviceSpy: jasmine.SpyObj<CourseGroupService>;

  function setup(id: string | null) {
    serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', [
      'getById',
      'create',
      'update',
    ]);
    serviceSpy.getById.and.returnValue(of(COURSE_GROUP));
    serviceSpy.create.and.returnValue(of(COURSE_GROUP));
    serviceSpy.update.and.returnValue(of(COURSE_GROUP));

    TestBed.configureTestingModule({
      imports: [CourseGroupForm],
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

    return TestBed.createComponent(CourseGroupForm);
  }

  describe('add mode', () => {
    it('starts in create mode without loading a course group', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeFalse();
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
        description: '資訊安全',
      });
      cmp.save();

      expect(serviceSpy.create).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 0, description: '資訊安全' }),
      );
      expect(navSpy).toHaveBeenCalledWith(['/course-groups', 2]);
    });
  });

  describe('edit mode', () => {
    it('loads the course group and patches values', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeTrue();
      expect(serviceSpy.getById).toHaveBeenCalledWith(2);
      expect(cmp.pkidDisplay()).toBe(2);
      expect(cmp.form.controls.description.value).toBe('網路管理');
    });

    it('updates with the loaded pkid on submit', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      cmp.form.patchValue({ description: '網路管理 2' });
      cmp.save();

      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 2, description: '網路管理 2' }),
      );
    });
  });
});
