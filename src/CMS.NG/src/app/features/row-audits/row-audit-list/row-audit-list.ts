import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';

import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditListItem, RowAuditQuery } from '@core/models/row-audit.model';

const FILTERS_KEY = 'row-audit-list-filters';
const SORT_KEY = 'row-audit-list-sort';
const PAGE_KEY = 'row-audit-list-page';

interface SortState {
  sortField: string | null;
  sortOrder: number;
}
interface PageState {
  first: number;
  rows: number;
}

/**
 * Read-only global audit log: every RowAudit row across all tables, newest first, with
 * table / action / keyword filters. No create/edit/delete — the log is append-only.
 */
@Component({
  selector: 'app-row-audit-list',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    SelectModule,
    TagModule,
  ],
  templateUrl: './row-audit-list.html',
  styleUrl: './row-audit-list.scss',
})
export class RowAuditList implements OnInit {
  private readonly service = inject(RowAuditService);
  private readonly messages = inject(MessageService);

  readonly items = signal<RowAuditListItem[]>([]);
  readonly loading = signal(false);
  readonly drawerVisible = signal(false);
  readonly tableNames = signal<string[]>([]);

  readonly actionOptions = [
    { label: '新增 Insert', value: 'Insert' },
    { label: '修改 Update', value: 'Update' },
    { label: '刪除 Delete', value: 'Delete' },
  ];

  // Two-way binding adapter for p-drawer [(visible)].
  get drawerVisibleModel(): boolean {
    return this.drawerVisible();
  }
  set drawerVisibleModel(value: boolean) {
    this.drawerVisible.set(value);
  }

  filters: RowAuditQuery = { tableName: null, actionType: null, keyword: null };

  sortState: SortState = { sortField: null, sortOrder: 1 };
  pageState: PageState = { first: 0, rows: 20 };
  readonly rowsPerPageOptions = [10, 20, 50];

  ngOnInit(): void {
    this.restoreState();
    this.service.getTableNames().subscribe({
      next: (names) => this.tableNames.set(names),
      error: () => {
        /* the table filter just stays empty */
      },
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service
      .query(this.filters)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (rows) => this.items.set(rows),
        error: () =>
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得異動紀錄。' }),
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
    this.filters = { tableName: null, actionType: null, keyword: null };
    sessionStorage.removeItem(FILTERS_KEY);
    this.applyFilters();
  }

  refresh(): void {
    this.load();
  }

  onSort(event: { field?: string | null; order?: number }): void {
    this.sortState = { sortField: event.field ?? null, sortOrder: event.order ?? 1 };
    sessionStorage.setItem(SORT_KEY, JSON.stringify(this.sortState));
  }

  onPage(event: TableLazyLoadEvent): void {
    this.pageState = { first: event.first ?? 0, rows: event.rows ?? this.pageState.rows };
    sessionStorage.setItem(PAGE_KEY, JSON.stringify(this.pageState));
  }

  /** Tag colour per action type. */
  actionSeverity(action: string): 'success' | 'info' | 'danger' | 'secondary' {
    switch (action) {
      case 'Insert':
        return 'success';
      case 'Update':
        return 'info';
      case 'Delete':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  private restoreState(): void {
    for (const [key, apply] of [
      [FILTERS_KEY, (v: RowAuditQuery) => (this.filters = { ...this.filters, ...v })],
      [SORT_KEY, (v: SortState) => (this.sortState = { ...this.sortState, ...v })],
      [PAGE_KEY, (v: PageState) => (this.pageState = { ...this.pageState, ...v })],
    ] as const) {
      const raw = sessionStorage.getItem(key);
      if (!raw) continue;
      try {
        apply(JSON.parse(raw));
      } catch {
        /* ignore corrupt state */
      }
    }
  }
}
