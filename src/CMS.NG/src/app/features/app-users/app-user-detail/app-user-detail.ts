import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';

@Component({
  selector: 'app-app-user-detail',
  standalone: true,
  imports: [DatePipe, ButtonModule, TagModule],
  templateUrl: './app-user-detail.html',
  styleUrl: './app-user-detail.scss',
})
export class AppUserDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly lookups = inject(LookupService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly user = signal<AppUser | null>(null);
  readonly loading = signal(true);
  private readonly roles = signal<AppRoleLookup[]>([]);

  // Labels for the assigned roles.
  readonly roleLabels = computed(() => {
    const map = new Map(this.roles().map((r) => [r.roleId, r.label]));
    return (this.user()?.roleIds ?? []).map((id) => map.get(id) ?? id);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      user: this.service.getById(id),
      roles: this.lookups.getAppRoles(),
    }).subscribe({
      next: ({ user, roles }) => {
        this.user.set(user);
        this.roles.set(roles);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得使用者資料。' });
      },
    });
  }

  edit(): void {
    const id = this.user()?.userId;
    if (id) this.router.navigate(['/app-users', id, 'edit']);
  }

  back(): void {
    this.router.navigate(['/app-users']);
  }

  confirmResetPassword(): void {
    const id = this.user()?.userId;
    if (!id) return;
    this.confirmation.confirm({
      header: '重設密碼',
      message: `確定要將使用者「${id}」的密碼重設為預設密碼？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => this.resetPassword(id),
    });
  }

  private resetPassword(id: string): void {
    this.service.resetPassword(id).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已重設', detail: '密碼已重設為預設密碼。' });
        this.service.getById(id).subscribe((u) => this.user.set(u));
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '重設失敗', detail: '無法重設密碼。' }),
    });
  }
}
