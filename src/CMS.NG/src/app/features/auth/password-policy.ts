import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Mirrors the backend `PasswordPolicy` (CMS.API). Shown bilingually in the UI.
export const PASSWORD_COMPLEXITY_MESSAGE_ZH =
  '密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號';
export const PASSWORD_COMPLEXITY_MESSAGE_EN =
  'Password must be at least 8 characters and contain at least 3 of the 4 classes: uppercase / lowercase / digit / symbol.';

/** At least 8 chars AND at least 3 of the 4 classes: uppercase, lowercase, digit, symbol. */
export function isPasswordComplex(password: string): boolean {
  if (!password || password.length < 8) return false;

  let classes = 0;
  if (/[A-Z]/.test(password)) classes++;
  if (/[a-z]/.test(password)) classes++;
  if (/[0-9]/.test(password)) classes++;
  if (/[^A-Za-z0-9]/.test(password)) classes++; // symbol = anything not a letter/digit
  return classes >= 3;
}

/** Control validator → `{ complexity: true }` when the new password is too weak. */
export function passwordComplexityValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    isPasswordComplex(control.value ?? '') ? null : { complexity: true };
}

/** Group validator → `{ mismatch: true }` when the two named controls differ. */
export function passwordsMatchValidator(newKey: string, confirmKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const newValue = group.get(newKey)?.value;
    const confirmValue = group.get(confirmKey)?.value;
    return newValue === confirmValue ? null : { mismatch: true };
  };
}
