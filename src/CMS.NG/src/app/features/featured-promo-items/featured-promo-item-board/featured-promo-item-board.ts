import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { ConfirmationService, MessageService } from 'primeng/api';

import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { FeaturedPromoItem } from '@core/models/featured-promo-item.model';
import { TrainingCenterLookup } from '@core/models/training-center-lookup.model';
import {
  CellContext,
  FeaturedPromoItemForm,
  FormSeed,
} from '../featured-promo-item-form/featured-promo-item-form';
import { addDays, dayHeading, mmdd, mondayOf, todayIso } from './date-week.util';

const CENTER_KEY = 'featured-promo-item-center';
const WEEK_KEY = 'featured-promo-item-week';

/** The cell currently open in the inline form. */
interface EditState {
  scheduleOn: string;
  slot: number;
  item: FeaturedPromoItem | null;
  seed: FormSeed | null;
}

/**
 * 上稿作業 board: a TrainingCenter-tab + Monday–Sunday week grid with 3 slots per day. Each cell is
 * either filled (PromoCode / Topic / Description with Edit·Copy·Delete·▲▼) or empty (Edit·Paste).
 */
@Component({
  selector: 'app-featured-promo-item-board',
  standalone: true,
  imports: [FeaturedPromoItemForm],
  templateUrl: './featured-promo-item-board.html',
  styleUrl: './featured-promo-item-board.scss',
})
export class FeaturedPromoItemBoard implements OnInit {
  private readonly service = inject(FeaturedPromoItemService);
  private readonly lookups = inject(LookupService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly slots = [1, 2, 3] as const;

  readonly centers = signal<TrainingCenterLookup[]>([]);
  readonly activeCenter = signal<number | null>(null);
  readonly weekStart = signal<string>(mondayOf(todayIso()));
  readonly items = signal<FeaturedPromoItem[]>([]);
  readonly loading = signal(false);
  readonly editing = signal<EditState | null>(null);
  readonly copyBuffer = signal<FormSeed | null>(null);

  /** The seven ISO dates Monday→Sunday for the active week. */
  readonly days = computed(() => Array.from({ length: 7 }, (_, i) => addDays(this.weekStart(), i)));
  readonly weekLabel = computed(() => `${mmdd(this.weekStart())} -- ${mmdd(addDays(this.weekStart(), 6))}`);

  ngOnInit(): void {
    this.restoreState();
    this.loadCenters();
  }

  // ---- data ----------------------------------------------------------

  private loadCenters(): void {
    this.lookups.getTrainingCenters().subscribe({
      next: (centers) => {
        this.centers.set(centers);
        if (this.activeCenter() === null && centers.length) {
          this.activeCenter.set(centers[0].pkid);
          sessionStorage.setItem(CENTER_KEY, String(centers[0].pkid));
        }
        this.load();
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得訓練中心。' }),
    });
  }

  load(): void {
    const center = this.activeCenter();
    if (center === null) return;
    this.loading.set(true);
    this.service.query({ trainingCenterPkid: center, weekStart: this.weekStart() }).subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得上稿資料。' });
      },
    });
  }

  // ---- navigation ----------------------------------------------------

  selectCenter(pkid: number): void {
    if (pkid === this.activeCenter()) return;
    this.activeCenter.set(pkid);
    sessionStorage.setItem(CENTER_KEY, String(pkid));
    this.editing.set(null);
    this.load();
  }

  prevWeek(): void {
    this.shiftWeek(-7);
  }

  nextWeek(): void {
    this.shiftWeek(7);
  }

  private shiftWeek(deltaDays: number): void {
    this.weekStart.set(addDays(this.weekStart(), deltaDays));
    sessionStorage.setItem(WEEK_KEY, this.weekStart());
    this.editing.set(null);
    this.load();
  }

  // ---- grid helpers --------------------------------------------------

  heading(day: string, index: number): string {
    return dayHeading(day, index);
  }

  itemAt(day: string, slot: number): FeaturedPromoItem | null {
    return this.items().find((i) => i.scheduleOn === day && i.slot === slot) ?? null;
  }

  isEditing(day: string, slot: number): boolean {
    const e = this.editing();
    return e !== null && e.scheduleOn === day && e.slot === slot;
  }

  contextFor(day: string, slot: number): CellContext {
    return { scheduleOn: day, trainingCenterPkid: this.activeCenter()!, slot };
  }

  // ---- cell actions --------------------------------------------------

  startEdit(day: string, slot: number, item: FeaturedPromoItem | null): void {
    this.editing.set({ scheduleOn: day, slot, item, seed: null });
  }

  startPaste(day: string, slot: number): void {
    const seed = this.copyBuffer();
    if (!seed) return;
    this.editing.set({ scheduleOn: day, slot, item: null, seed });
  }

  copy(item: FeaturedPromoItem): void {
    this.copyBuffer.set({ promoCode: item.promoCode, topic: item.topic, description: item.description });
    this.messages.add({ severity: 'info', summary: '已複製', detail: `「${item.topic}」已複製，可於空版位貼上。` });
  }

  onSaved(): void {
    this.editing.set(null);
    this.load();
  }

  onCancelled(): void {
    this.editing.set(null);
  }

  move(item: FeaturedPromoItem, direction: number): void {
    this.service.move(item.pkid, direction).subscribe({
      next: () => this.load(),
      error: (err: HttpErrorResponse) => {
        const detail = err.status === 400 ? '已達版位邊界，無法移動。' : '移動失敗，請稍後再試。';
        this.messages.add({ severity: 'warn', summary: '無法移動', detail });
      },
    });
  }

  confirmDelete(item: FeaturedPromoItem): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除版位 <b>${item.slot}</b>「${item.topic}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(item),
    });
  }

  private remove(item: FeaturedPromoItem): void {
    this.service.delete(item.pkid).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已刪除', detail: `「${item.topic}」已刪除。` });
        this.editing.set(null);
        this.load();
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除上稿資料。' }),
    });
  }

  private restoreState(): void {
    const center = sessionStorage.getItem(CENTER_KEY);
    if (center) this.activeCenter.set(Number(center));
    const week = sessionStorage.getItem(WEEK_KEY);
    if (week) this.weekStart.set(mondayOf(week));
  }
}
