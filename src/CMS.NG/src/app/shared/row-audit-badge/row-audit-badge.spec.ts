import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { RowAuditBadge } from './row-audit-badge';
import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';

const TRAIL: RowAuditEntry[] = [
  { dateTime: '2026-06-04T14:30:00', userName: 'alice', actionType: 'Update', actionDesc: 'Title' },
  { dateTime: '2026-06-01T09:00:00', userName: 'bob', actionType: 'Insert', actionDesc: 'Intro' },
];

describe('RowAuditBadge', () => {
  let serviceSpy: jasmine.SpyObj<RowAuditService>;

  function setup(entries: RowAuditEntry[], pkid: number | null = 123) {
    serviceSpy = jasmine.createSpyObj<RowAuditService>('RowAuditService', ['getForRecord']);
    serviceSpy.getForRecord.and.returnValue(of(entries));

    TestBed.configureTestingModule({
      imports: [RowAuditBadge],
      providers: [provideNoopAnimations(), { provide: RowAuditService, useValue: serviceSpy }],
    });

    const fixture = TestBed.createComponent(RowAuditBadge);
    fixture.componentRef.setInput('tableName', 'Course');
    fixture.componentRef.setInput('pkid', pkid);
    fixture.detectChanges();
    return fixture;
  }

  it('fetches this record and shows the latest (most recent) entry inline', () => {
    const fixture = setup(TRAIL);

    expect(serviceSpy.getForRecord).toHaveBeenCalledWith('Course', 123);
    expect(fixture.componentInstance.latest()?.userName).toBe('alice');

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('異動紀錄 History');
    expect(text).toContain('Update by alice');
  });

  it('opens a dialog listing the full trail when the badge is clicked', () => {
    const fixture = setup(TRAIL);

    const badge = (fixture.nativeElement as HTMLElement).querySelector(
      '.row-audit-badge',
    ) as HTMLButtonElement;
    badge.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.dialogVisible()).toBeTrue();

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.audit-table tbody tr');
    expect(rows.length).toBe(2);

    const dialogText = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(dialogText).toContain('alice');
    expect(dialogText).toContain('bob');
  });

  it('renders the empty state when there is no history', () => {
    const fixture = setup([]);

    expect(fixture.componentInstance.hasHistory()).toBeFalse();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('no history');
  });

  it('shows the empty state and issues no request for an unsaved record (no pkid)', () => {
    const fixture = setup(TRAIL, null);

    expect(serviceSpy.getForRecord).not.toHaveBeenCalled();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('no history');
  });
});
