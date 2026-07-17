import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/auth/auth.service';
import {
  PASSWORD_COMPLEXITY_MESSAGE,
  passwordComplexityValidator,
  passwordsMatchValidator,
} from '@core/auth/password-policy';

/**
 * "My Profile" — lets the signed-in user view their UserId and roles (read-only), edit only their own
 * UserName, and change their own password. The account is resolved server-side from the JWT; UserId and
 * roles cannot be changed. No password hash is ever sent to or from the client.
 */
@Component({
  selector: 'app-profile',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    TagModule,
    ToastModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly messageService = inject(MessageService);

  /** The complexity rule text, shown as a hint and as the validation error. */
  readonly complexityMessage = PASSWORD_COMPLEXITY_MESSAGE;

  readonly saving = signal(false);
  readonly changingPassword = signal(false);
  /** Server-side rejection message (e.g. wrong current password) shown inline under the form. */
  readonly passwordError = signal('');

  /** Read-only identity — sourced from the stored profile / decoded token. */
  readonly userId = computed(() => this.auth.profile()?.userId ?? '');
  readonly roles = this.auth.roles;

  readonly form = this.fb.nonNullable.group({
    userName: [this.auth.userName(), [Validators.required, Validators.maxLength(200)]],
  });

  readonly passwordForm = this.fb.nonNullable.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, passwordComplexityValidator()]],
      confirmNewPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator('newPassword', 'confirmNewPassword') },
  );

  save(): void {
    const userName = this.form.controls.userName.value.trim();
    if (!userName) {
      this.form.controls.userName.setErrors({ required: true });
      this.form.markAllAsTouched();
      this.messageService.add({ severity: 'warn', summary: '欄位未完成', detail: '請輸入使用者名稱。' });
      return;
    }

    this.saving.set(true);
    this.auth.updateProfile(userName).subscribe({
      next: (profile) => {
        this.saving.set(false);
        this.form.controls.userName.setValue(profile.userName);
        this.form.markAsPristine();
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: '個人資料已更新。' });
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail: '無法更新個人資料，請稍後再試。' });
      },
    });
  }

  changePassword(): void {
    this.passwordError.set('');

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      this.messageService.add({
        severity: 'warn',
        summary: '欄位未完成',
        detail: '請確認密碼欄位符合規則。Please check the password fields.',
      });
      return;
    }

    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.getRawValue();

    this.changingPassword.set(true);
    this.auth.changePassword(currentPassword, newPassword, confirmNewPassword).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordForm.reset();
        this.messageService.add({ severity: 'success', summary: '已更新', detail: '密碼已更新。Password changed.' });
      },
      error: (err: HttpErrorResponse) => {
        this.changingPassword.set(false);
        const message =
          (err.error?.message as string) ?? '無法變更密碼，請稍後再試。Unable to change the password.';
        this.passwordError.set(message);
        this.messageService.add({ severity: 'error', summary: '變更失敗', detail: message });
      },
    });
  }
}
