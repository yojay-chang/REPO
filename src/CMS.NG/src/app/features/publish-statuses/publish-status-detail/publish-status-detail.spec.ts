import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';

import { PublishStatusDetail } from './publish-status-detail';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const STATUS: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

describe('PublishStatusDetail', () => {
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;

  function setup(id = '2') {
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', ['getById']);
    serviceSpy.getById.and.returnValue(of(STATUS));

    TestBed.configureTestingModule({
      imports: [PublishStatusDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
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

    return TestBed.createComponent(PublishStatusDetail);
  }

  it('loads the status by numeric pkid', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(cmp.status()?.description).toBe('已發布');
  });

  it('renders the status fields in the template', () => {
    const fixture = setup();
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('已發布');
  });
});
