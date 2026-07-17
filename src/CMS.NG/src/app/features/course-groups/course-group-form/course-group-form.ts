import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { CourseGroupRequest } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-course-group-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    RowAuditBadge,
  ],
  templateUrl: './course-group-form.html',
  styleUrl: './course-group-form.scss',
})
export class CourseGroupForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messageService = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  // pkid is a system-assigned IDENTITY — displayed read-only in edit mode, never entered.
  readonly pkidDisplay = signal<number | null>(null);

  private pkid: number | null = null;

  readonly form = this.fb.nonNullable.group({
    description: ['', [Validators.required, Validators.maxLength(100)]],
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
      next: (courseGroup) => {
        this.pkidDisplay.set(courseGroup.pkid);
        this.form.patchValue({
          description: courseGroup.description,
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
    const request: CourseGroupRequest = {
      pkid: this.pkid ?? 0, // IDENTITY: ignored on create, matched on update.
      description: raw.description,
    };

    this.saving.set(true);
    const op$ = this.isEdit() ? this.service.update(request) : this.service.create(request);

    op$.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: `課程群組「${saved.description}」已儲存。` });
        this.router.navigate(['/course-groups', saved.pkid]);
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.pkid !== null) {
      this.router.navigate(['/course-groups', this.pkid]);
    } else {
      this.router.navigate(['/course-groups']);
    }
  }
}
