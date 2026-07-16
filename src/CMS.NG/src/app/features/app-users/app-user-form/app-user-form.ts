import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of, Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserService } from '@core/services/app-user.service';
import { AuthService } from '@core/services/auth.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser, AppUserRequest } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-app-user-form',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    CheckboxModule,
    MultiSelectModule,
    RowAuditBadge,
  ],
  templateUrl: './app-user-form.html',
  styleUrl: './app-user-form.scss',
})
export class AppUserForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly auth = inject(AuthService);
  private readonly lookups = inject(LookupService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  /** Reset-to-default is Admin-only (the backend enforces it too). */
  readonly isAdmin = this.auth.isAdmin;

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly roles = signal<AppRoleLookup[]>([]);
  readonly passwordUpdatedTime = signal<string | null>(null);

  /** The record's pkid (int) for the audit badge; null until an existing user is loaded. */
  readonly auditPkid = signal<number | null>(null);

  private userId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    userId: ['', [Validators.required, Validators.maxLength(200)]],
    userName: ['', [Validators.required, Validators.maxLength(200)]],
    isActive: [true],
    roleIds: this.fb.nonNullable.control<string[]>([]),
  });

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.userId);

    forkJoin({
      roles: this.lookups.getAppRoles(),
      user: this.userId ? this.service.getById(this.userId) : of(null),
    }).subscribe({
      next: ({ roles, user }) => {
        this.roles.set(roles);
        if (user) {
          this.form.patchValue({
            userId: user.userId,
            userName: user.userName,
            isActive: user.isActive,
            roleIds: user.roleIds,
          });
          this.form.controls.userId.disable(); // UserId is the PK — immutable on edit.
          this.passwordUpdatedTime.set(user.passwordUpdatedTime ?? null);
          this.auditPkid.set(user.pkid);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得表單資料。' });
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: AppUserRequest = {
      userId: raw.userId,
      userName: raw.userName,
      isActive: raw.isActive,
      roleIds: raw.roleIds,
    };

    this.saving.set(true);
    const op$: Observable<AppUser | void> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit() ? '已更新' : '已新增',
          detail: `使用者「${request.userId}」已儲存。`,
        });
        this.router.navigate(['/app-users', request.userId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? `使用者代碼「${request.userId}」已存在。` : '儲存失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  confirmResetPassword(): void {
    if (!this.userId) return;
    const id = this.userId;
    this.confirmation.confirm({
      header: '重設密碼',
      message: `確定要將使用者「${id}」的密碼重設為預設密碼？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => {
        // Admin-only endpoint; the client sends only the UserId (never a password/hash).
        this.auth.resetPasswordToDefault(id).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: '已重設', detail: '密碼已重設為預設密碼。' });
            this.service.getById(id).subscribe((u) => this.passwordUpdatedTime.set(u.passwordUpdatedTime ?? null));
          },
          error: () =>
            this.messages.add({ severity: 'error', summary: '重設失敗', detail: '無法重設密碼。' }),
        });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/app-users']);
  }

  invalid(control: 'userId' | 'userName'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
