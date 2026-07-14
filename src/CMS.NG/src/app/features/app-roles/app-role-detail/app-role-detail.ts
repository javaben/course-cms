import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';

import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { AppRole } from '@core/models/app-role.model';
import { AppUserLookup } from '@core/models/app-user-lookup.model';

@Component({
  selector: 'app-app-role-detail',
  standalone: true,
  imports: [ButtonModule, TagModule],
  templateUrl: './app-role-detail.html',
  styleUrl: './app-role-detail.scss',
})
export class AppRoleDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  readonly role = signal<AppRole | null>(null);
  readonly loading = signal(true);
  private readonly users = signal<AppUserLookup[]>([]);

  // Labels for the assigned users.
  readonly userLabels = computed(() => {
    const map = new Map(this.users().map((u) => [u.userId, u.label]));
    return (this.role()?.userIds ?? []).map((id) => map.get(id) ?? id);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      role: this.service.getById(id),
      users: this.lookups.getAppUsers(),
    }).subscribe({
      next: ({ role, users }) => {
        this.role.set(role);
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得角色資料。' });
      },
    });
  }

  edit(): void {
    const id = this.role()?.roleId;
    if (id) this.router.navigate(['/app-roles', id, 'edit']);
  }

  back(): void {
    this.router.navigate(['/app-roles']);
  }
}
