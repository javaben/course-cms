import { Component, Input, OnChanges, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';

import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';

/**
 * Reusable "異動紀錄 History" badge for a single record. Given a `tableName` and the record's `pkid`
 * it loads that record's RowAudit trail, shows the most-recent entry inline on the badge, and opens a
 * dialog with the full trail (newest first) when clicked. When there is no pkid yet (e.g. a brand-new
 * form) or no history, it shows a neutral "no history" state and makes no request.
 */
@Component({
  selector: 'app-row-audit-badge',
  standalone: true,
  imports: [ButtonModule, DialogModule],
  templateUrl: './row-audit-badge.html',
  styleUrl: './row-audit-badge.scss',
  providers: [DatePipe],
})
export class RowAuditBadge implements OnChanges {
  @Input({ required: true }) tableName!: string;
  @Input() pkid: number | string | null | undefined;

  private readonly service = inject(RowAuditService);
  private readonly datePipe = inject(DatePipe);

  readonly entries = signal<RowAuditEntry[]>([]);
  readonly loaded = signal(false);
  readonly dialogVisible = signal(false);

  /** Most recent entry (the list is newest-first), or null when there is no history. */
  readonly latest = computed<RowAuditEntry | null>(() => this.entries()[0] ?? null);
  readonly hasHistory = computed(() => this.entries().length > 0);

  ngOnChanges(): void {
    this.load();
  }

  load(): void {
    if (!this.tableName || this.pkid == null || this.pkid === '' || this.pkid === 0) {
      // No identified record yet (e.g. a new form) — nothing to fetch.
      this.entries.set([]);
      this.loaded.set(true);
      return;
    }

    this.service.getForRecord(this.tableName, this.pkid).subscribe({
      next: (rows) => {
        this.entries.set(rows ?? []);
        this.loaded.set(true);
      },
      error: () => {
        this.entries.set([]);
        this.loaded.set(true);
      },
    });
  }

  open(): void {
    this.dialogVisible.set(true);
  }

  /** Formats an entry's datetime as "yyyy-MM-dd HH:mm". */
  formatDate(value: string): string {
    return this.datePipe.transform(value, 'yyyy-MM-dd HH:mm') ?? '';
  }

  /** Inline one-line summary of the latest change, e.g. "Update by alice · 2026-06-04 14:30". */
  latestSummary(): string | null {
    const e = this.latest();
    return e ? `${e.actionType} by ${e.userName} · ${this.formatDate(e.dateTime)}` : null;
  }
}
