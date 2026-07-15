import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { MultiSelectModule } from 'primeng/multiselect';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

import { AppUserRequest, AppRoleLookup } from '@core/models/app-user.model';
import { AppUserService } from '@core/services/app-user.service';
import { AuthService } from '@core/auth/auth.service';

interface RoleOption {
  roleId: string;
  label: string;
}

@Component({
  selector: 'app-app-user-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    ToggleSwitchModule,
    MultiSelectModule,
    ToastModule,
    ConfirmDialogModule,
  ],
  templateUrl: './app-user-form.html',
  styleUrl: './app-user-form.scss',
})
export class AppUserForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly auth = inject(AuthService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly resetting = signal(false);
  readonly roleOptions = signal<RoleOption[]>([]);

  /** Whether the signed-in user is an Admin — gates the "reset password" action (backend also enforces it). */
  readonly isAdmin = this.auth.isAdmin;

  private userId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    userId: ['', [Validators.required, Validators.maxLength(200)]],
    userName: ['', [Validators.required, Validators.maxLength(200)]],
    isActive: [true],
    roleIds: [[] as string[]],
  });

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.userId);

    const user$ = this.userId ? this.service.getById(this.userId) : of(null);

    forkJoin({ user: user$, roles: this.service.getAppRoles() }).subscribe({
      next: ({ user, roles }) => {
        this.roleOptions.set(this.buildOptions(roles));
        if (user) {
          this.form.patchValue({
            userId: user.userId,
            userName: user.userName,
            isActive: user.isActive,
            roleIds: user.roleIds,
          });
          this.form.controls.userId.disable();
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
      },
    });
  }

  private buildOptions(roles: AppRoleLookup[]): RoleOption[] {
    return roles.map((r) => ({ roleId: r.roleId, label: `${r.roleName} (${r.roleId})` }));
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.messageService.add({ severity: 'warn', summary: '欄位未完成', detail: '請確認必填欄位。' });
      return;
    }

    // getRawValue() includes the disabled userId control in edit mode.
    const raw = this.form.getRawValue();
    const request: AppUserRequest = {
      userId: raw.userId,
      userName: raw.userName,
      isActive: raw.isActive,
      roleIds: raw.roleIds,
    };

    this.saving.set(true);
    const op$ = this.isEdit() ? this.service.update(request) : this.service.create(request);

    op$.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: `使用者「${saved.userId}」已儲存。` });
        this.router.navigate(['/app-users', saved.userId]);
      },
      error: (err) => {
        this.saving.set(false);
        const detail = err?.status === 409 ? '使用者代碼已存在。' : '儲存失敗，請稍後再試。';
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.userId) {
      this.router.navigate(['/app-users', this.userId]);
    } else {
      this.router.navigate(['/app-users']);
    }
  }

  /** Confirm, then reset the edited user's password to the system default (Admin only). */
  confirmResetPassword(): void {
    if (!this.userId) return;
    const targetId = this.userId;
    this.confirmationService.confirm({
      header: '重設密碼',
      message: `確定要將使用者「${targetId}」的密碼重設為系統預設密碼？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => this.resetPassword(targetId),
    });
  }

  private resetPassword(userId: string): void {
    // Only the userId is sent; no password or hash ever crosses the wire.
    this.resetting.set(true);
    this.service.resetPassword(userId).subscribe({
      next: () => {
        this.resetting.set(false);
        this.messageService.add({
          severity: 'success',
          summary: '已重設',
          detail: `使用者「${userId}」的密碼已重設為系統預設密碼。`,
        });
      },
      error: (err) => {
        this.resetting.set(false);
        const detail = err?.status === 403 ? '僅限管理員執行此操作。' : '無法重設密碼，請稍後再試。';
        this.messageService.add({ severity: 'error', summary: '重設失敗', detail });
      },
    });
  }
}
