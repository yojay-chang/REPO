import { FormControl, FormGroup } from '@angular/forms';
import {
  isPasswordComplex,
  passwordComplexityValidator,
  passwordsMatchValidator,
  PASSWORD_COMPLEXITY_MESSAGE,
} from './password-policy';

describe('password-policy', () => {
  describe('isPasswordComplex', () => {
    it('rejects passwords shorter than 8 characters even with 3+ classes', () => {
      expect(isPasswordComplex('Ab1!')).toBeFalse();
      expect(isPasswordComplex('Aa1')).toBeFalse();
    });

    it('rejects long passwords using fewer than 3 character classes', () => {
      expect(isPasswordComplex('alllowercase')).toBeFalse(); // 1 class
      expect(isPasswordComplex('lowercase123')).toBeFalse(); // 2 classes (lower + digit)
      expect(isPasswordComplex('PASSWORD1')).toBeFalse(); // 2 classes (upper + digit)
    });

    it('accepts length >= 8 with at least 3 of the 4 classes', () => {
      expect(isPasswordComplex('Abcdef1!')).toBeTrue(); // 4 classes
      expect(isPasswordComplex('Abcdefg1')).toBeTrue(); // upper + lower + digit
      expect(isPasswordComplex('abcdefg1!')).toBeTrue(); // lower + digit + symbol
    });
  });

  describe('passwordComplexityValidator', () => {
    const validate = passwordComplexityValidator();

    it('returns no error for an empty value (leaves it to required)', () => {
      expect(validate(new FormControl(''))).toBeNull();
    });

    it('flags a weak password with a complexity error', () => {
      expect(validate(new FormControl('weak'))).toEqual({ complexity: true });
    });

    it('passes a strong password', () => {
      expect(validate(new FormControl('Abcdef1!'))).toBeNull();
    });
  });

  describe('passwordsMatchValidator', () => {
    const group = (next: string, confirm: string) =>
      new FormGroup({
        newPassword: new FormControl(next),
        confirmNewPassword: new FormControl(confirm),
      });
    const validate = passwordsMatchValidator('newPassword', 'confirmNewPassword');

    it('flags a mismatch once both fields have values', () => {
      expect(validate(group('Abcdef1!', 'Different1!'))).toEqual({ mismatch: true });
    });

    it('passes when the two values are equal', () => {
      expect(validate(group('Abcdef1!', 'Abcdef1!'))).toBeNull();
    });

    it('does not flag while a field is still empty', () => {
      expect(validate(group('Abcdef1!', ''))).toBeNull();
    });
  });

  it('exposes the bilingual complexity message', () => {
    expect(PASSWORD_COMPLEXITY_MESSAGE).toContain('8');
    expect(PASSWORD_COMPLEXITY_MESSAGE).toContain('大寫');
  });
});
