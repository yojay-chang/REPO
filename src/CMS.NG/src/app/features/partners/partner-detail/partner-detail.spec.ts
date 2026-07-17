import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { PartnerDetail } from './partner-detail';
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

describe('PartnerDetail', () => {
  let serviceSpy: jasmine.SpyObj<PartnerService>;

  function setup(id = '2') {
    serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['getById']);
    serviceSpy.getById.and.returnValue(of(PARTNER));

    TestBed.configureTestingModule({
      imports: [PartnerDetail],
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

    return TestBed.createComponent(PartnerDetail);
  }

  it('loads the partner by numeric pkid', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(cmp.partner()?.name).toBe('思科');
  });

  it('renders the partner fields in the template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('思科');
    expect(text).toContain('CISCO');
  });

  it('viewCourses navigates to the courses list filtered by partner', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.viewCourses();
    expect(navSpy).toHaveBeenCalledWith(['/courses'], { queryParams: { partnerPkid: 2 } });
  });
});
