import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';

import { AuthService } from '@core/auth/auth.service';
import {
  PASSWORD_COMPLEXITY_MESSAGE,
  passwordComplexityValidator,
  passwordsMatchValidator,
} from '@core/auth/password-policy';

/**
 * Forced first-login password change. Reached only via `authGuard`, which pins a user whose account is
 * still on the system default password to this route (the backend refuses every other endpoint for their
 * token, so there is nothing else for them to do). Rendered outside the nav shell — the only ways out are
 * changing the password or logging out.
 *
 * On success the service stores the fresh token returned by the backend, which clears
 * `auth.mustChangePassword()` and releases the rest of the app.
 */
@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule, ButtonModule, PasswordModule, MessageModule],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
})
export class ChangePassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** The complexity rule text, shown as a hint and as the validation error. */
  readonly complexityMessage = PASSWORD_COMPLEXITY_MESSAGE;

  readonly submitting = signal(false);
  /** Server-side rejection (e.g. reusing the default password) shown inline above the button. */
  readonly errorMessage = signal('');

  readonly userName = this.auth.userName;

  readonly form = this.fb.nonNullable.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, passwordComplexityValidator()]],
      confirmNewPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator('newPassword', 'confirmNewPassword') },
  );

  submit(): void {
    this.errorMessage.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmNewPassword } = this.form.getRawValue();

    this.submitting.set(true);
    this.auth.changePassword(currentPassword, newPassword, confirmNewPassword).subscribe({
      next: () => {
        this.submitting.set(false);
        // The stored token no longer carries the restriction — the guard now lets the app through.
        void this.router.navigate(['/']);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(
          (err.error?.message as string) ?? '無法變更密碼，請稍後再試。Unable to change the password.',
        );
      },
    });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
