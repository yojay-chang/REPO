import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { PublishStatusList } from './publish-status-list';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const STATUSES: PublishStatus[] = [
  { pkid: 1, description: '草稿', isDraft: true, isPublished: false, isDiscontinued: false },
  { pkid: 2, description: '已發布', isDraft: false, isPublished: true, isDiscontinued: false },
];

describe('PublishStatusList', () => {
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;

  function setup() {
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(STATUSES));
    serviceSpy.delete.and.returnValue(of(void 0));

    TestBed.configureTestingModule({
      imports: [PublishStatusList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: PublishStatusService, useValue: serviceSpy },
      ],
    });

    return TestBed.createComponent(PublishStatusList);
  }

  beforeEach(() => sessionStorage.clear());

  it('loads statuses on init', () => {
    const fixture = setup();
    fixture.detectChanges();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(fixture.componentInstance.statuses().length).toBe(2);
  });

  it('renders a row per status', () => {
    const fixture = setup();
    fixture.detectChanges();
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilter persists filters and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = '草稿';
    cmp.applyFilter();

    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(cmp.filterVisible()).toBeFalse();
    expect(sessionStorage.getItem('publish-status-list-filters')).toContain('草稿');
  });

  it('clearFilter resets the filter and removes stored filters', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.filter.keyword = '草稿';
    cmp.applyFilter();
    cmp.clearFilter();

    expect(cmp.filter.keyword).toBeNull();
    expect(sessionStorage.getItem('publish-status-list-filters')).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.view(STATUSES[0]);
    expect(navSpy).toHaveBeenCalledWith(['/publish-statuses', 1]);
  });

  it('edit navigates to the edit route', () => {
    const fixture = setup();
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');

    fixture.componentInstance.edit(STATUSES[0]);
    expect(navSpy).toHaveBeenCalledWith(['/publish-statuses', 1, 'edit']);
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    fixture.componentInstance.confirmDelete(STATUSES[0]);

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
