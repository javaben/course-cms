import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/services/auth.service';

/** Public login page. Posts { userId, password } to /api/Auth/login and stores the profile. */
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    userId: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  ngOnInit(): void {
    // Already signed in? Skip the form.
    if (this.auth.isAuthenticated()) {
      void this.router.navigateByUrl('/');
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl('/');
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const detail =
          err.status === 401 ? '帳號或密碼錯誤。' : '登入失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '登入失敗', detail });
      },
    });
  }

  invalid(control: 'userId' | 'password'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
