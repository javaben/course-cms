import { FormControl, FormGroup } from '@angular/forms';
import {
  isPasswordComplex,
  passwordComplexityValidator,
  passwordsMatchValidator,
} from './password-policy';

describe('password-policy', () => {
  describe('isPasswordComplex', () => {
    it('accepts length >= 8 with at least 3 of 4 classes', () => {
      expect(isPasswordComplex('Abcdefg1')).toBeTrue(); // upper+lower+digit
      expect(isPasswordComplex('NewPass#1')).toBeTrue(); // 4 classes
      expect(isPasswordComplex('PASSword!')).toBeTrue(); // upper+lower+symbol
    });

    it('rejects short passwords (< 8) even with 4 classes', () => {
      expect(isPasswordComplex('Ab1#')).toBeFalse();
      expect(isPasswordComplex('Abc1#67')).toBeFalse(); // 7 chars
    });

    it('rejects passwords with fewer than 3 classes', () => {
      expect(isPasswordComplex('abcdefgh')).toBeFalse(); // 1 class
      expect(isPasswordComplex('abcdefg1')).toBeFalse(); // 2 classes
      expect(isPasswordComplex('Abcdefgh')).toBeFalse(); // 2 classes
    });

    it('rejects empty/blank', () => {
      expect(isPasswordComplex('')).toBeFalse();
    });
  });

  describe('passwordComplexityValidator', () => {
    const validate = passwordComplexityValidator();

    it('returns null for a strong password', () => {
      expect(validate(new FormControl('NewPass#1'))).toBeNull();
    });

    it('returns { complexity: true } for a weak password', () => {
      expect(validate(new FormControl('weak'))).toEqual({ complexity: true });
    });
  });

  describe('passwordsMatchValidator', () => {
    const group = () =>
      new FormGroup(
        { newPassword: new FormControl('NewPass#1'), confirmNewPassword: new FormControl('') },
        { validators: passwordsMatchValidator('newPassword', 'confirmNewPassword') },
      );

    it('flags a mismatch', () => {
      const g = group();
      g.controls.confirmNewPassword.setValue('Different#1');
      expect(g.errors).toEqual({ mismatch: true });
    });

    it('passes when they match', () => {
      const g = group();
      g.controls.confirmNewPassword.setValue('NewPass#1');
      expect(g.errors).toBeNull();
    });
  });
});
