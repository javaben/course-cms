import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-partner-detail',
  standalone: true,
  imports: [ButtonModule, RowAuditBadge],
  templateUrl: './partner-detail.html',
  styleUrl: './partner-detail.scss',
})
export class PartnerDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messages = inject(MessageService);

  readonly partner = signal<Partner | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(id).subscribe({
      next: (partner) => {
        this.partner.set(partner);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得合作廠商資料。' });
      },
    });
  }

  edit(): void {
    const id = this.partner()?.pkid;
    if (id != null) this.router.navigate(['/partners', id, 'edit']);
  }

  back(): void {
    this.router.navigate(['/partners']);
  }
}
