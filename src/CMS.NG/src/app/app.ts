import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';

import { AuthService } from '@core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route?: string;
}

interface NavGroup {
  label: string;
  icon: string;
  expanded: boolean;
  items: NavItem[];
}

// The 系統管理 Admin group is shown only to users whose roles include this.
const ADMIN_GROUP_LABEL = '系統管理 Admin';
const ADMIN_ROLE = 'Admin';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule, ConfirmDialogModule, ButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly collapsed = signal(false);

  /** UserName of the signed-in user, shown in the shell header. */
  readonly userName = computed(() => this.auth.profile()?.userName ?? '');

  // Full sidebar nav.
  readonly groups = signal<NavGroup[]>([
    {
      label: '首頁管理 Home',
      icon: 'pi pi-home',
      expanded: true,
      items: [
        {
          label: '上稿作業 FeaturedPromoItem',
          icon: 'pi pi-calendar',
          route: '/featured-promo-items',
        },
      ],
    },
    {
      label: '課程管理 Course',
      icon: 'pi pi-folder',
      expanded: false,
      items: [
        { label: '課程 Course', icon: 'pi pi-book', route: '/courses' },
        { label: '合作廠商 Partner', icon: 'pi pi-building', route: '/partners' },
        { label: '課程群組 CourseGroup', icon: 'pi pi-sitemap', route: '/course-groups' },
      ],
    },
    { label: '說明會 Seminar', icon: 'pi pi-comments', expanded: false, items: [] },
    { label: '活動管理 Promotion', icon: 'pi pi-megaphone', expanded: false, items: [] },
    { label: '線上報名 Forms', icon: 'pi pi-pencil', expanded: false, items: [] },
    { label: '網站資訊 WebInfo', icon: 'pi pi-globe', expanded: false, items: [] },
    { label: '考試中心 TestingCenter', icon: 'pi pi-check-circle', expanded: false, items: [] },
    {
      label: ADMIN_GROUP_LABEL,
      icon: 'pi pi-shield',
      expanded: true,
      items: [
        { label: '角色 AppRole', icon: 'pi pi-id-card', route: '/app-roles' },
        { label: '使用者 AppUser', icon: 'pi pi-user', route: '/app-users' },
        { label: '發布狀態 PublishStatus', icon: 'pi pi-flag', route: '/publish-statuses' },
        { label: '異動紀錄 RowAudit', icon: 'pi pi-history', route: '/row-audits' },
      ],
    },
  ]);

  /** Nav with the Admin group hidden unless the user's roles include "Admin". */
  readonly visibleGroups = computed(() =>
    this.groups().filter((g) => g.label !== ADMIN_GROUP_LABEL || this.auth.hasRole(ADMIN_ROLE)),
  );

  toggleCollapsed(): void {
    this.collapsed.update((c) => !c);
  }

  toggleGroup(target: NavGroup): void {
    this.groups.update((groups) =>
      groups.map((g) => (g === target ? { ...g, expanded: !g.expanded } : g)),
    );
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
