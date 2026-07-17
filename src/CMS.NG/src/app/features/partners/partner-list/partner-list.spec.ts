import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { PartnerList } from './partner-list';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

const PARTNERS: Partner[] = [
  { pkid: 1, name: '微軟', appKey: 'MS', nameOnPartnerMenu: '微軟原廠課程', nameOnCourseDetailPage: '微軟', displayOrder: 1, imageFilename: 'ms.png' },
  { pkid: 2, name: '思科', appKey: 'CISCO', nameOnPartnerMenu: '思科網路課程', nameOnCourseDetailPage: '思科', displayOrder: 2, imageFilename: null },
];

describe('PartnerList', () => {
  let serviceSpy: jasmine.SpyObj<PartnerService>;

  function setup() {
    serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(PARTNERS));
    serviceSpy.delete.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [PartnerList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: PartnerService, useValue: serviceSpy },
      ],
    });

    return TestBed.createComponent(PartnerList);
  }

  beforeEach(() => sessionStorage.clear());

  it('loads partners on init', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(fixture.componentInstance.partners().length).toBe(2);
  });

  it('renders a row per partner', () => {
    const fixture = setup();
    fixture.detectChanges();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilter persists filters and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = '微軟';
    cmp.applyFilter();

    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(cmp.filterVisible()).toBeFalse();
    expect(sessionStorage.getItem('partner-list-filters')).toContain('微軟');
  });

  it('clearFilter resets the filter and removes stored filters', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = '微軟';
    cmp.applyFilter();
    cmp.clearFilter();

    expect(cmp.filter.keyword).toBeNull();
    expect(sessionStorage.getItem('partner-list-filters')).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.view(PARTNERS[0]);
    expect(navSpy).toHaveBeenCalledWith(['/partners', 1]);
  });

  it('edit navigates to the edit route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.edit(PARTNERS[0]);
    expect(navSpy).toHaveBeenCalledWith(['/partners', 1, 'edit']);
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    fixture.componentInstance.confirmDelete(PARTNERS[0]);

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
