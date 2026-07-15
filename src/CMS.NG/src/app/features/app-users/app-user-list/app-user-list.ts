import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserService } from '@core/services/app-user.service';
import { AppUser, AppUserQuery } from '@core/models/app-user.model';

const FILTERS_KEY = 'app-user-list-filters';
const SORT_KEY = 'app-user-list-sort';
const PAGE_KEY = 'app-user-list-page';

interface SortState {
  sortField: string | null;
  sortOrder: number;
}
interface PageState {
  first: number;
  rows: number;
}

@Component({
  selector: 'app-app-user-list',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    SelectModule,
    TagModule,
    TooltipModule,
  ],
  templateUrl: './app-user-list.html',
  styleUrl: './app-user-list.scss',
})
export class AppUserList implements OnInit {
  private readonly service = inject(AppUserService);
  private readonly router = inject(Router);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly users = signal<AppUser[]>([]);
  readonly loading = signal(false);
  readonly drawerVisible = signal(false);

  // Two-way binding adapter for p-drawer [(visible)].
  get drawerVisibleModel(): boolean {
    return this.drawerVisible();
  }
  set drawerVisibleModel(value: boolean) {
    this.drawerVisible.set(value);
  }

  // Filter model (bound in the drawer).
  filters: AppUserQuery = { keyword: null, isActive: null };

  readonly activeOptions = [
    { label: '全部', value: null },
    { label: '啟用', value: true },
    { label: '停用', value: false },
  ];

  // Persisted table state.
  sortState: SortState = { sortField: null, sortOrder: 1 };
  pageState: PageState = { first: 0, rows: 20 };
  readonly rowsPerPageOptions = [10, 20, 50];

  ngOnInit(): void {
    this.restoreState();
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service
      .query(this.filters)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (rows) => this.users.set(rows),
        error: () =>
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得使用者資料。' }),
      });
  }

  applyFilters(): void {
    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.filters));
    this.pageState = { ...this.pageState, first: 0 };
    sessionStorage.setItem(PAGE_KEY, JSON.stringify(this.pageState));
    this.drawerVisible.set(false);
    this.load();
  }

  resetFilters(): void {
    this.filters = { keyword: null, isActive: null };
    sessionStorage.removeItem(FILTERS_KEY);
    this.applyFilters();
  }

  onSort(event: { field?: string | null; order?: number }): void {
    this.sortState = {
      sortField: event.field ?? null,
      sortOrder: event.order ?? 1,
    };
    sessionStorage.setItem(SORT_KEY, JSON.stringify(this.sortState));
  }

  onPage(event: TableLazyLoadEvent): void {
    this.pageState = {
      first: event.first ?? 0,
      rows: event.rows ?? this.pageState.rows,
    };
    sessionStorage.setItem(PAGE_KEY, JSON.stringify(this.pageState));
  }

  view(user: AppUser): void {
    this.router.navigate(['/app-users', user.userId]);
  }

  edit(user: AppUser): void {
    this.router.navigate(['/app-users', user.userId, 'edit']);
  }

  add(): void {
    this.router.navigate(['/app-users/new']);
  }

  confirmDelete(user: AppUser): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${user.pkid}</b>「${user.userId}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(user),
    });
  }

  private remove(user: AppUser): void {
    this.service.delete(user.userId).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已刪除', detail: `使用者「${user.userId}」已刪除。` });
        this.load();
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除使用者。' }),
    });
  }

  private restoreState(): void {
    const filters = sessionStorage.getItem(FILTERS_KEY);
    if (filters) {
      try {
        this.filters = { ...this.filters, ...JSON.parse(filters) };
      } catch {
        /* ignore corrupt state */
      }
    }
    const sort = sessionStorage.getItem(SORT_KEY);
    if (sort) {
      try {
        this.sortState = { ...this.sortState, ...JSON.parse(sort) };
      } catch {
        /* ignore */
      }
    }
    const page = sessionStorage.getItem(PAGE_KEY);
    if (page) {
      try {
        this.pageState = { ...this.pageState, ...JSON.parse(page) };
      } catch {
        /* ignore */
      }
    }
  }
}
