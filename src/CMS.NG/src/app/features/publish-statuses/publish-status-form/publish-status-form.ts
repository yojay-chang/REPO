import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { PublishStatusRequest } from '@core/models/publish-status.model';
import { PublishStatusService } from '@core/services/publish-status.service';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-publish-status-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    ToggleSwitchModule,
    ToastModule,
    RowAuditBadge,
  ],
  templateUrl: './publish-status-form.html',
  styleUrl: './publish-status-form.scss',
})
export class PublishStatusForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PublishStatusService);
  private readonly messageService = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  /** The edited record's numeric pkid (null when creating) — drives the row-audit history badge. */
  readonly recordPkid = signal<number | null>(null);

  private pkid: number | null = null;

  readonly form = this.fb.nonNullable.group({
    pkid: [null as number | null, [Validators.required, Validators.min(0), Validators.max(255)]],
    description: ['', [Validators.required, Validators.maxLength(50)]],
    isDraft: [false],
    isPublished: [false],
    isDiscontinued: [false],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.pkid = idParam === null ? null : Number(idParam);
    this.isEdit.set(this.pkid !== null);
    this.recordPkid.set(this.pkid);

    if (this.pkid === null) {
      this.loading.set(false);
      return;
    }

    this.service.getById(this.pkid).subscribe({
      next: (status) => {
        this.form.patchValue({
          pkid: status.pkid,
          description: status.description,
          isDraft: status.isDraft,
          isPublished: status.isPublished,
          isDiscontinued: status.isDiscontinued,
        });
        this.form.controls.pkid.disable();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.messageService.add({ severity: 'warn', summary: '欄位未完成', detail: '請確認必填欄位。' });
      return;
    }

    // getRawValue() includes the disabled pkid control in edit mode.
    const raw = this.form.getRawValue();
    const request: PublishStatusRequest = {
      pkid: raw.pkid!,
      description: raw.description,
      isDraft: raw.isDraft,
      isPublished: raw.isPublished,
      isDiscontinued: raw.isDiscontinued,
    };

    this.saving.set(true);
    const op$ = this.isEdit() ? this.service.update(request) : this.service.create(request);

    op$.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: `發布狀態「${saved.description}」已儲存。` });
        this.router.navigate(['/publish-statuses', saved.pkid]);
      },
      error: (err) => {
        this.saving.set(false);
        const detail = err?.status === 409 ? '主代碼已存在。' : '儲存失敗，請稍後再試。';
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.pkid !== null) {
      this.router.navigate(['/publish-statuses', this.pkid]);
    } else {
      this.router.navigate(['/publish-statuses']);
    }
  }
}
