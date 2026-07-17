import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';

import { RowAuditEntry } from '@core/models/row-audit.model';
import { RowAuditService } from '@core/services/row-audit.service';

type TagSeverity = 'success' | 'info' | 'danger' | 'secondary';

/**
 * Reusable row-audit history badge. Drop it into a page's toolbar with the audited `tableName` and the
 * current record's numeric `pkid`; it fetches that record's audit trail (newest first) and shows the
 * latest change inline. Clicking opens a dialog with the full trail. An unsaved record (no `pkid`) or a
 * record with no history shows a neutral empty state and issues no request.
 */
@Component({
  selector: 'app-row-audit-badge',
  imports: [DatePipe, DialogModule, TagModule],
  templateUrl: './row-audit-badge.html',
  styleUrl: './row-audit-badge.scss',
})
export class RowAuditBadge {
  private readonly service = inject(RowAuditService);

  /** The audited business table, e.g. `"Course"`. */
  readonly tableName = input.required<string>();
  /** The record's numeric pkid; null on a new/unsaved record. */
  readonly pkid = input<number | null>(null);

  readonly entries = signal<RowAuditEntry[]>([]);
  readonly loading = signal(false);
  readonly dialogVisible = signal(false);

  /** The most recent entry (the trail is newest-first), or null when there is no history. */
  readonly latest = computed(() => this.entries()[0] ?? null);
  readonly hasHistory = computed(() => this.entries().length > 0);

  constructor() {
    // Re-fetch whenever the target record changes; skip unsaved records (no pkid yet).
    effect(() => {
      const tableName = this.tableName();
      const pkid = this.pkid();
      if (!tableName || pkid == null || pkid <= 0) {
        this.entries.set([]);
        return;
      }
      this.fetch(tableName, pkid);
    });
  }

  openDialog(): void {
    this.dialogVisible.set(true);
  }

  actionSeverity(actionType: string): TagSeverity {
    switch (actionType) {
      case 'Insert':
        return 'success';
      case 'Update':
        return 'info';
      case 'Delete':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  private fetch(tableName: string, pkid: number): void {
    this.loading.set(true);
    this.service.getForRecord(tableName, pkid).subscribe({
      next: (entries) => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => {
        this.entries.set([]);
        this.loading.set(false);
      },
    });
  }
}
