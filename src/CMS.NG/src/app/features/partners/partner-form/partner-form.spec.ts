import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { PartnerForm } from './partner-form';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

const PARTNER: Partner = {
  pkid: 2,
  name: '思科',
  appKey: 'CISCO',
  nameOnPartnerMenu: '思科網路課程',
  nameOnCourseDetailPage: '思科',
  displayOrder: 2,
  imageFilename: null,
};

describe('PartnerForm', () => {
  let serviceSpy: jasmine.SpyObj<PartnerService>;

  function setup(id: string | null) {
    serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', [
      'getById',
      'create',
      'update',
    ]);
    serviceSpy.getById.and.returnValue(of(PARTNER));
    serviceSpy.create.and.returnValue(of(PARTNER));
    serviceSpy.update.and.returnValue(of(PARTNER));

    TestBed.configureTestingModule({
      imports: [PartnerForm],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        { provide: PartnerService, useValue: serviceSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => id } } },
        },
      ],
    });

    return TestBed.createComponent(PartnerForm);
  }

  describe('add mode', () => {
    it('starts in create mode without loading a partner', () => {
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
        name: '甲骨文',
        appKey: 'ORCL',
        nameOnPartnerMenu: '甲骨文資料庫課程',
        nameOnCourseDetailPage: '甲骨文',
        displayOrder: 10,
        imageFilename: null,
      });
      cmp.save();

      expect(serviceSpy.create).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 0, name: '甲骨文', appKey: 'ORCL' }),
      );
      expect(navSpy).toHaveBeenCalledWith(['/partners', 2]);
    });
  });

  describe('edit mode', () => {
    it('loads the partner and patches values', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      expect(cmp.isEdit()).toBeTrue();
      expect(serviceSpy.getById).toHaveBeenCalledWith(2);
      expect(cmp.pkidDisplay()).toBe(2);
      expect(cmp.form.controls.name.value).toBe('思科');
    });

    it('updates with the loaded pkid on submit', () => {
      const fixture = setup('2');
      fixture.detectChanges();
      const cmp = fixture.componentInstance;

      cmp.form.patchValue({ name: '思科 2', displayOrder: 5 });
      cmp.save();

      expect(serviceSpy.update).toHaveBeenCalledWith(
        jasmine.objectContaining({ pkid: 2, name: '思科 2', displayOrder: 5 }),
      );
    });
  });
});
