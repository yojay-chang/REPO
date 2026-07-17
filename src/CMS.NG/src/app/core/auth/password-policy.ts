import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Client-side mirror of the backend {@code PasswordPolicy} (CMS.API/Auth/PasswordPolicy.cs).
 * A password is valid when it is at least 8 characters long AND uses at least three of the four
 * character classes: uppercase, lowercase, digit, symbol. The server remains authoritative.
 */
export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_REQUIRED_CLASSES = 3;

/** Bilingual message shown when a new password fails the complexity rule (matches the backend). */
export const PASSWORD_COMPLEXITY_MESSAGE =
  '密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號';

/** True when {@code value} satisfies the length and class-count rule. */
export function isPasswordComplex(value: string): boolean {
  if (!value || value.length < PASSWORD_MIN_LENGTH) return false;

  const hasUpper = /[A-Z]/.test(value);
  const hasLower = /[a-z]/.test(value);
  const hasDigit = /[0-9]/.test(value);
  // Anything that is not an ASCII letter or digit counts as a symbol.
  const hasSymbol = /[^A-Za-z0-9]/.test(value);

  const classes = [hasUpper, hasLower, hasDigit, hasSymbol].filter(Boolean).length;
  return classes >= PASSWORD_REQUIRED_CLASSES;
}

/** Reactive-forms validator: sets a {@code complexity} error when the new password is too weak. */
export function passwordComplexityValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string;
    if (!value) return null; // let `required` report emptiness
    return isPasswordComplex(value) ? null : { complexity: true };
  };
}

/**
 * Group-level validator: sets a {@code mismatch} error on the group when the new password and its
 * confirmation differ (only once both fields have a value).
 */
export function passwordsMatchValidator(
  newControlName: string,
  confirmControlName: string,
): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const next = group.get(newControlName)?.value as string;
    const confirm = group.get(confirmControlName)?.value as string;
    if (!next || !confirm) return null;
    return next === confirm ? null : { mismatch: true };
  };
}
