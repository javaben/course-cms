import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

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

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule, ConfirmDialogModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly collapsed = signal(false);

  // Sidebar nav — only 系統管理 Admin > 角色 AppRole is wired to a route for now.
  readonly groups = signal<NavGroup[]>([
    { label: '首頁管理 Home', icon: 'pi pi-home', expanded: false, items: [] },
    { label: '課程管理 Course', icon: 'pi pi-folder', expanded: false, items: [] },
    { label: '說明會 Seminar', icon: 'pi pi-comments', expanded: false, items: [] },
    { label: '活動管理 Promotion', icon: 'pi pi-megaphone', expanded: false, items: [] },
    { label: '線上報名 Forms', icon: 'pi pi-pencil', expanded: false, items: [] },
    { label: '網站資訊 WebInfo', icon: 'pi pi-globe', expanded: false, items: [] },
    { label: '考試中心 TestingCenter', icon: 'pi pi-check-circle', expanded: false, items: [] },
    {
      label: '系統管理 Admin',
      icon: 'pi pi-shield',
      expanded: true,
      items: [
        { label: '角色 AppRole', icon: 'pi pi-id-card', route: '/app-roles' },
        { label: '使用者 AppUser', icon: 'pi pi-user', route: '/app-users' },
      ],
    },
  ]);

  toggleCollapsed(): void {
    this.collapsed.update((c) => !c);
  }

  toggleGroup(target: NavGroup): void {
    this.groups.update((groups) =>
      groups.map((g) => (g === target ? { ...g, expanded: !g.expanded } : g)),
    );
  }
}
