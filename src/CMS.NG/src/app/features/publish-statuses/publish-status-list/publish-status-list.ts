import { Component, OnInit, inject, signal } from '@angular/core';
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

import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus, PublishStatusQuery } from '@core/models/publish-status.model';

const FILTERS_KEY = 'publish-status-list-filters';
const SORT_KEY = 'publish-status-list-sort';
const PAGE_KEY = 'publish-status-list-page';

interface SortState {
  sortField: string | null;
  sortOrder: number;
}
interface PageState {
  first: number;
  rows: number;
}

@Component({
  selector: 'app-publish-status-list',
  standalone: true,
  imports: [
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
  templateUrl: './publish-status-list.html',
  styleUrl: './publish-status-list.scss',
})
export class PublishStatusList implements OnInit {
  private readonly service = inject(PublishStatusService);
  private readonly router = inject(Router);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly statuses = signal<PublishStatus[]>([]);
  readonly loading = signal(false);
  readonly drawerVisible = signal(false);

  // Tri-state bool filter options for the drawer p-selects.
  readonly boolOptions = [
    { label: '是', value: true },
    { label: '否', value: false },
  ];

  // Two-way binding adapter for p-drawer [(visible)].
  get drawerVisibleModel(): boolean {
    return this.drawerVisible();
  }
  set drawerVisibleModel(value: boolean) {
    this.drawerVisible.set(value);
  }

  // Filter model (bound in the drawer).
  filters: PublishStatusQuery = {
    keyword: null,
    isDraft: null,
    isPublished: null,
    isDiscontinued: null,
  };

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
        next: (rows) => this.statuses.set(rows),
        error: () =>
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得發布狀態資料。' }),
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
    this.filters = { keyword: null, isDraft: null, isPublished: null, isDiscontinued: null };
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

  view(status: PublishStatus): void {
    this.router.navigate(['/publish-statuses', status.pkid]);
  }

  edit(status: PublishStatus): void {
    this.router.navigate(['/publish-statuses', status.pkid, 'edit']);
  }

  add(): void {
    this.router.navigate(['/publish-statuses/new']);
  }

  confirmDelete(status: PublishStatus): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${status.pkid}</b>「${status.description}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(status),
    });
  }

  private remove(status: PublishStatus): void {
    this.service.delete(status.pkid).subscribe({
      next: () => {
        this.messages.add({
          severity: 'success',
          summary: '已刪除',
          detail: `發布狀態「${status.description}」已刪除。`,
        });
        this.load();
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除發布狀態。' }),
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
