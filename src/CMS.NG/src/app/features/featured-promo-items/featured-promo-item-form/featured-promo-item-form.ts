import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';

import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import {
  FeaturedPromoItem,
  FeaturedPromoItemRequest,
} from '@core/models/featured-promo-item.model';

/** The cell (day + slot + center) an inline form is being opened against. */
export interface CellContext {
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
}

/** Values a Paste seeds a new form with (copied from another cell). */
export interface FormSeed {
  promoCode: string;
  topic: string;
  description: string;
}

/**
 * Inline edit / new form for a single FeaturedPromoItem cell. Enter a Promotion2.PromoCode; it is
 * looked up (setting Promotion_pkid, and pre-filling empty Topic/Description). Persists via the
 * service and emits {@link saved}; Cancel emits {@link cancelled}.
 */
@Component({
  selector: 'app-featured-promo-item-form',
  standalone: true,
  imports: [ReactiveFormsModule, InputTextModule, RowAuditBadge],
  templateUrl: './featured-promo-item-form.html',
  styleUrl: './featured-promo-item-form.scss',
})
export class FeaturedPromoItemForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(FeaturedPromoItemService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  /** Existing item when editing; null for a new (empty) cell. */
  @Input() item: FeaturedPromoItem | null = null;
  /** The cell being edited (day + center + slot). */
  @Input({ required: true }) context!: CellContext;
  /** Copied values when the form is opened via Paste. */
  @Input() seed: FormSeed | null = null;

  @Output() readonly saved = new EventEmitter<void>();
  @Output() readonly cancelled = new EventEmitter<void>();

  readonly saving = signal(false);
  readonly lookupError = signal<string | null>(null);
  private promotionPkid = 0;

  readonly form = this.fb.nonNullable.group({
    promoCode: ['', [Validators.required, Validators.maxLength(30)]],
    topic: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.required, Validators.maxLength(300)]],
  });

  get isEdit(): boolean {
    return this.item !== null;
  }

  ngOnInit(): void {
    if (this.item) {
      this.promotionPkid = this.item.promotionPkid;
      this.form.patchValue({
        promoCode: this.item.promoCode,
        topic: this.item.topic,
        description: this.item.description,
      });
    } else if (this.seed) {
      this.form.patchValue({
        promoCode: this.seed.promoCode,
        topic: this.seed.topic,
        description: this.seed.description,
      });
    }
  }

  /** Resolve the entered PromoCode → Promotion_pkid, pre-filling empty Topic/Description. */
  onPromoCodeBlur(): void {
    const code = this.form.controls.promoCode.value.trim();
    if (!code) {
      this.promotionPkid = 0;
      this.lookupError.set(null);
      return;
    }

    this.lookups.getPromotionByCode(code).subscribe({
      next: (promo) => {
        this.promotionPkid = promo.pkid;
        this.lookupError.set(null);
        if (!this.form.controls.topic.value) this.form.controls.topic.setValue(promo.topic);
        if (!this.form.controls.description.value)
          this.form.controls.description.setValue(promo.description);
      },
      error: () => {
        this.promotionPkid = 0;
        this.lookupError.set('找不到此活動代碼。');
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const code = this.form.controls.promoCode.value.trim();
    this.saving.set(true);

    // Authoritative resolve of PromoCode → Promotion_pkid before persisting.
    this.lookups.getPromotionByCode(code).subscribe({
      next: (promo) => {
        this.promotionPkid = promo.pkid;
        this.lookupError.set(null);
        this.persist();
      },
      error: () => {
        this.saving.set(false);
        this.promotionPkid = 0;
        this.lookupError.set('找不到此活動代碼。');
      },
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  invalid(control: 'promoCode' | 'topic' | 'description'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }

  private persist(): void {
    const raw = this.form.getRawValue();
    const request: FeaturedPromoItemRequest = {
      pkid: this.item?.pkid ?? 0,
      scheduleOn: this.context.scheduleOn,
      trainingCenterPkid: this.context.trainingCenterPkid,
      slot: this.context.slot,
      promotionPkid: this.promotionPkid,
      topic: raw.topic,
      description: raw.description,
    };

    const op$: Observable<FeaturedPromoItem | void> = this.isEdit
      ? this.service.update(request)
      : this.service.create(request);
    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit ? '已更新' : '已新增',
          detail: `版位 ${request.slot}「${request.topic}」已儲存。`,
        });
        this.saved.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? '此日期版位已被使用。' : '儲存失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }
}
