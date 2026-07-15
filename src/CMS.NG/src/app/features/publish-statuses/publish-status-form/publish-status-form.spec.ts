import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { PublishStatusForm } from './publish-status-form';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const STATUS: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

describe('PublishStatusForm', () => {
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;

  function setup(id: string | null) {
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', [
      'getById',
      'create',
      'update',
    ]);
    serviceSpy.getById.and.returnValue(of(STATUS));
    serviceSpy.create.and.returnValue(of(STATUS));
    serviceSpy.update.and.returnValue(of(STATUS));

    TestBed.configureTestingModule({
      imports: [PublishStatusForm],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: PublishStatusService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(PublishStatusForm);
  }

  describe('add mode', () => {
    it('starts in create mode with the pkid control enabled', () => {
      const fixture = setup(null);
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeFalse();
      expect(cmp.form.controls.pkid.enabled).toBeTrue();
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
        pkid: 10,
        description: '審核中',
        isDraft: true,
        isPublished: false,
        isDiscontinued: false,
      });
      cmp.save();

      expect(serviceSpy.create).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 10, description: '審核中', isDraft: true }),
      );
      expect(navSpy).toHaveBeenCalledWith(['/publish-statuses', 2]);
    });
  });

  describe('edit mode', () => {
    it('loads the status, disables pkid, and patches values', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeTrue();
      expect(serviceSpy.getById).toHaveBeenCalledWith(2);
      expect(cmp.form.controls.pkid.disabled).toBeTrue();
      expect(cmp.form.controls.description.value).toBe('已發布');
      expect(cmp.form.getRawValue().pkid).toBe(2);
    });

    it('updates including the disabled pkid via getRawValue', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      cmp.form.patchValue({ description: '已發布 2', isDiscontinued: true });
      cmp.save();

      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 2, description: '已發布 2', isDiscontinued: true }),
      );
    });
  });
});
