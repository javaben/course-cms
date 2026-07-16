import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';

import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-publish-status-detail',
  standalone: true,
  imports: [ButtonModule, TagModule, RowAuditBadge],
  templateUrl: './publish-status-detail.html',
  styleUrl: './publish-status-detail.scss',
})
export class PublishStatusDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PublishStatusService);
  private readonly messages = inject(MessageService);

  readonly status = signal<PublishStatus | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(id).subscribe({
      next: (status) => {
        this.status.set(status);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得發布狀態資料。' });
      },
    });
  }

  edit(): void {
    const id = this.status()?.pkid;
    if (id != null) this.router.navigate(['/publish-statuses', id, 'edit']);
  }

  back(): void {
    this.router.navigate(['/publish-statuses']);
  }
}
