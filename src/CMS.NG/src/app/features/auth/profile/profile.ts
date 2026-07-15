import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/services/auth.service';
import {
  PASSWORD_COMPLEXITY_MESSAGE_EN,
  PASSWORD_COMPLEXITY_MESSAGE_ZH,
  passwordComplexityValidator,
  passwordsMatchValidator,
} from '@features/auth/password-policy';

/**
 * "My Profile" — shows the signed-in user's UserId and roles read-only, and lets them edit ONLY
 * their own UserName. Identity/roles are never sent for change; the server derives the user from
 * the JWT. On save the shell header and session storage refresh via AuthService.
 */
@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, TagModule],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly saving = signal(false);
  readonly changingPassword = signal(false);

  readonly complexityMessageZh = PASSWORD_COMPLEXITY_MESSAGE_ZH;
  readonly complexityMessageEn = PASSWORD_COMPLEXITY_MESSAGE_EN;

  // Read-only identity + roles, sourced from the stored profile / decoded token.
  readonly userId = computed(() => this.auth.profile()?.userId ?? '');
  readonly roles = this.auth.roles;

  readonly form = this.fb.nonNullable.group({
    userName: [this.auth.profile()?.userName ?? '', [Validators.required]],
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
      this.form.controls.userName.markAsTouched();
      return;
    }

    this.saving.set(true);
    this.auth.updateUserName(userName).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.form.controls.userName.setValue(res.userName); // canonical, trimmed
        this.messages.add({ severity: 'success', summary: '已更新', detail: '使用者名稱已更新。' });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail = err.status === 400 ? '使用者名稱為必填。' : '更新失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '更新失敗', detail });
      },
    });
  }

  invalid(): boolean {
    const c = this.form.controls.userName;
    return c.invalid && (c.touched || c.dirty);
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.changingPassword.set(true);
    this.auth.changePassword(this.passwordForm.getRawValue()).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordForm.reset();
        this.messages.add({ severity: 'success', summary: '已更新', detail: '密碼已變更。' });
      },
      error: (err: HttpErrorResponse) => {
        this.changingPassword.set(false);
        // Surface the server's specific message (wrong current password / complexity / mismatch).
        const detail =
          (err.error && typeof err.error === 'object' && 'message' in err.error
            ? (err.error as { message: string }).message
            : null) ?? '變更失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '變更失敗', detail });
      },
    });
  }

  /** Shows the complexity hint once the new-password field is touched and failing the rule. */
  showComplexityError(): boolean {
    const c = this.passwordForm.controls.newPassword;
    return c.hasError('complexity') && (c.touched || c.dirty);
  }

  /** Shows the mismatch hint once the confirm field is touched and the two differ. */
  showMismatchError(): boolean {
    const c = this.passwordForm.controls.confirmNewPassword;
    return this.passwordForm.hasError('mismatch') && (c.touched || c.dirty);
  }

  passwordRequired(control: 'currentPassword' | 'newPassword' | 'confirmNewPassword'): boolean {
    const c = this.passwordForm.controls[control];
    return c.hasError('required') && (c.touched || c.dirty);
  }
}
