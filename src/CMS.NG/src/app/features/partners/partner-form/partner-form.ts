import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { PartnerRequest } from '@core/models/partner.model';
import { PartnerService } from '@core/services/partner.service';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-partner-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    ToastModule,
    RowAuditBadge,
  ],
  templateUrl: './partner-form.html',
  styleUrl: './partner-form.scss',
})
export class PartnerForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messageService = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  // pkid is a system-assigned IDENTITY — displayed read-only in edit mode, never entered.
  readonly pkidDisplay = signal<number | null>(null);

  private pkid: number | null = null;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    appKey: ['', [Validators.required, Validators.maxLength(10)]],
    nameOnPartnerMenu: ['', [Validators.required, Validators.maxLength(200)]],
    nameOnCourseDetailPage: ['', [Validators.required, Validators.maxLength(50)]],
    displayOrder: [0, [Validators.required]],
    imageFilename: this.fb.control<string | null>(null, [Validators.maxLength(50)]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.pkid = idParam === null ? null : Number(idParam);
    this.isEdit.set(this.pkid !== null);

    if (this.pkid === null) {
      this.loading.set(false);
      return;
    }

    this.service.getById(this.pkid).subscribe({
      next: (partner) => {
        this.pkidDisplay.set(partner.pkid);
        this.form.patchValue({
          name: partner.name,
          appKey: partner.appKey,
          nameOnPartnerMenu: partner.nameOnPartnerMenu,
          nameOnCourseDetailPage: partner.nameOnCourseDetailPage,
          displayOrder: partner.displayOrder,
          imageFilename: partner.imageFilename,
        });
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

    const raw = this.form.getRawValue();
    const request: PartnerRequest = {
      pkid: this.pkid ?? 0, // IDENTITY: ignored on create, matched on update.
      name: raw.name,
      appKey: raw.appKey,
      nameOnPartnerMenu: raw.nameOnPartnerMenu,
      nameOnCourseDetailPage: raw.nameOnCourseDetailPage,
      displayOrder: raw.displayOrder,
      imageFilename: raw.imageFilename?.trim() ? raw.imageFilename.trim() : null,
    };

    this.saving.set(true);
    const op$ = this.isEdit() ? this.service.update(request) : this.service.create(request);

    op$.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: `合作廠商「${saved.name}」已儲存。` });
        this.router.navigate(['/partners', saved.pkid]);
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.pkid !== null) {
      this.router.navigate(['/partners', this.pkid]);
    } else {
      this.router.navigate(['/partners']);
    }
  }
}
