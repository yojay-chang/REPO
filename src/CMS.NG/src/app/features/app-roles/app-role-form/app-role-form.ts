import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { MultiSelectModule } from 'primeng/multiselect';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AppRoleRequest, AppUserLookup } from '@core/models/app-role.model';
import { AppRoleService } from '@core/services/app-role.service';

interface UserOption {
  userId: string;
  label: string;
}

@Component({
  selector: 'app-app-role-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    MultiSelectModule,
    ToastModule,
  ],
  templateUrl: './app-role-form.html',
  styleUrl: './app-role-form.scss',
})
export class AppRoleForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly messageService = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly userOptions = signal<UserOption[]>([]);

  private roleId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    roleId: ['', [Validators.required, Validators.maxLength(200)]],
    roleName: ['', [Validators.required, Validators.maxLength(200)]],
    permissionLevel: [100, [Validators.required]],
    description: [null as string | null, [Validators.maxLength(400)]],
    userIds: [[] as string[]],
  });

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.roleId);

    const role$ = this.roleId ? this.service.getById(this.roleId) : of(null);

    forkJoin({ role: role$, users: this.service.getAppUsers() }).subscribe({
      next: ({ role, users }) => {
        this.userOptions.set(this.buildOptions(users));
        if (role) {
          this.form.patchValue({
            roleId: role.roleId,
            roleName: role.roleName,
            permissionLevel: role.permissionLevel,
            description: role.description,
            userIds: role.userIds,
          });
          this.form.controls.roleId.disable();
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
      },
    });
  }

  private buildOptions(users: AppUserLookup[]): UserOption[] {
    return users.map((u) => ({ userId: u.userId, label: `${u.userName} (${u.userId})` }));
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.messageService.add({ severity: 'warn', summary: '欄位未完成', detail: '請確認必填欄位。' });
      return;
    }

    // getRawValue() includes the disabled roleId control in edit mode.
    const raw = this.form.getRawValue();
    const request: AppRoleRequest = {
      roleId: raw.roleId,
      roleName: raw.roleName,
      permissionLevel: raw.permissionLevel,
      description: raw.description,
      userIds: raw.userIds,
    };

    this.saving.set(true);
    const op$ = this.isEdit() ? this.service.update(request) : this.service.create(request);

    op$.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: `角色「${saved.roleId}」已儲存。` });
        this.router.navigate(['/app-roles', saved.roleId]);
      },
      error: (err) => {
        this.saving.set(false);
        const detail = err?.status === 409 ? '角色代碼已存在。' : '儲存失敗，請稍後再試。';
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.roleId) {
      this.router.navigate(['/app-roles', this.roleId]);
    } else {
      this.router.navigate(['/app-roles']);
    }
  }
}
