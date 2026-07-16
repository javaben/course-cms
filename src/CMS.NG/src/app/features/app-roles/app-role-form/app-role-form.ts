import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of, Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelectModule } from 'primeng/multiselect';
import { MessageService } from 'primeng/api';

import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { AppRole, AppRoleRequest } from '@core/models/app-role.model';
import { AppUserLookup } from '@core/models/app-user-lookup.model';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-app-role-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    MultiSelectModule,
    RowAuditBadge,
  ],
  templateUrl: './app-role-form.html',
  styleUrl: './app-role-form.scss',
})
export class AppRoleForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly users = signal<AppUserLookup[]>([]);

  /** The record's pkid (int) for the audit badge; null until an existing role is loaded. */
  readonly auditPkid = signal<number | null>(null);

  private roleId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    roleId: ['', [Validators.required, Validators.maxLength(200)]],
    roleName: ['', [Validators.required, Validators.maxLength(200)]],
    permissionLevel: [100, [Validators.required]],
    description: this.fb.control<string | null>(null, [Validators.maxLength(400)]),
    userIds: this.fb.nonNullable.control<string[]>([]),
  });

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.roleId);

    forkJoin({
      users: this.lookups.getAppUsers(),
      role: this.roleId ? this.service.getById(this.roleId) : of(null),
    }).subscribe({
      next: ({ users, role }) => {
        this.users.set(users);
        if (role) {
          this.form.patchValue({
            roleId: role.roleId,
            roleName: role.roleName,
            permissionLevel: role.permissionLevel,
            description: role.description ?? null,
            userIds: role.userIds,
          });
          this.auditPkid.set(role.pkid);
          this.form.controls.roleId.disable(); // RoleId is the PK — immutable on edit.
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
    const request: AppRoleRequest = {
      roleId: raw.roleId,
      roleName: raw.roleName,
      permissionLevel: raw.permissionLevel,
      description: raw.description,
      userIds: raw.userIds,
    };

    this.saving.set(true);
    const op$: Observable<AppRole | void> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit() ? '已更新' : '已新增',
          detail: `角色「${request.roleId}」已儲存。`,
        });
        this.router.navigate(['/app-roles', request.roleId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? `角色代碼「${request.roleId}」已存在。` : '儲存失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/app-roles']);
  }

  invalid(control: 'roleId' | 'roleName' | 'permissionLevel'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
