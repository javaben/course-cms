import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';

import { PartnerService } from '@core/services/partner.service';
import { Partner, PartnerQuery } from '@core/models/partner.model';

const FILTERS_KEY = 'partner-list-filters';
const SORT_KEY = 'partner-list-sort';
const PAGE_KEY = 'partner-list-page';

interface SortState {
  sortField: string | null;
  sortOrder: number;
}
interface PageState {
  first: number;
  rows: number;
}

@Component({
  selector: 'app-partner-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    TooltipModule,
  ],
  templateUrl: './partner-list.html',
  styleUrl: './partner-list.scss',
})
export class PartnerList implements OnInit {
  private readonly service = inject(PartnerService);
  private readonly router = inject(Router);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly partners = signal<Partner[]>([]);
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
  filters: PartnerQuery = { keyword: null };

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
        next: (rows) => this.partners.set(rows),
        error: () =>
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得合作廠商資料。' }),
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
    this.filters = { keyword: null };
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

  view(partner: Partner): void {
    this.router.navigate(['/partners', partner.pkid]);
  }

  edit(partner: Partner): void {
    this.router.navigate(['/partners', partner.pkid, 'edit']);
  }

  add(): void {
    this.router.navigate(['/partners/new']);
  }

  confirmDelete(partner: Partner): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${partner.pkid}</b>「${partner.name}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(partner),
    });
  }

  private remove(partner: Partner): void {
    this.service.delete(partner.pkid).subscribe({
      next: () => {
        this.messages.add({
          severity: 'success',
          summary: '已刪除',
          detail: `合作廠商「${partner.name}」已刪除。`,
        });
        this.load();
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除合作廠商。' }),
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
