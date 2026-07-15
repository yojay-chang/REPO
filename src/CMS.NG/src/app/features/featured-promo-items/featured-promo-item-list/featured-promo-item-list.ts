import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

import { FeaturedPromoItem, FeaturedPromoItemRequest } from '@core/models/featured-promo-item.model';
import { TrainingCenterLookup } from '@core/models/training-center.model';
import { PromotionLookup } from '@core/models/promotion.model';
import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { toIsoDate, addDays, startOfWeekMonday } from '@core/utils/date.util';
import {
  FeaturedPromoItemForm,
  FeaturedPromoItemFormValue,
} from '../featured-promo-item-form/featured-promo-item-form';

const TC_KEY = 'featured-promo-item-list-tc';
const WEEK_KEY = 'featured-promo-item-list-week';

const WEEKDAY_LABELS = ['日', '一', '二', '三', '四', '五', '六'];

/** A day column in the current week. */
interface DayCell {
  date: Date;
  iso: string;
  label: string;
}

/** Which slot is currently being inline-edited, and its seed values (null = blank New). */
interface EditingContext {
  scheduleOn: string;
  slot: number;
  pkid: number | null;
  value: FeaturedPromoItemFormValue | null;
}

/**
 * Weekly promo scheduler (spec ui-query). TrainingCenter tabs across the top filter
 * `TrainingCenter_pkid`; a Monday→Sunday week navigator filters `ScheduleOn`. Each day shows three
 * slots, each either an existing item (Edit / Copy / Delete / ± move) or an empty cell (Edit / Paste).
 */
@Component({
  selector: 'app-featured-promo-item-list',
  imports: [ButtonModule, ToastModule, ConfirmDialogModule, FeaturedPromoItemForm],
  templateUrl: './featured-promo-item-list.html',
  styleUrl: './featured-promo-item-list.scss',
})
export class FeaturedPromoItemList implements OnInit {
  private readonly service = inject(FeaturedPromoItemService);
  private readonly lookups = inject(LookupService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly slots = [1, 2, 3];

  readonly trainingCenters = signal<TrainingCenterLookup[]>([]);
  readonly promotions = signal<PromotionLookup[]>([]);
  readonly items = signal<FeaturedPromoItem[]>([]);
  readonly loading = signal(false);

  readonly activeTrainingCenterPkid = signal<number | null>(null);
  readonly weekStart = signal<Date>(startOfWeekMonday(new Date()));

  readonly editing = signal<EditingContext | null>(null);
  readonly clipboard = signal<FeaturedPromoItemFormValue | null>(null);

  /** The seven Monday→Sunday day columns for the active week. */
  readonly days = computed<DayCell[]>(() => {
    const start = this.weekStart();
    return Array.from({ length: 7 }, (_, i) => {
      const date = addDays(start, i);
      return {
        date,
        iso: toIsoDate(date)!,
        label: `${date.getMonth() + 1}/${date.getDate()} (${WEEKDAY_LABELS[date.getDay()]})`,
      };
    });
  });

  ngOnInit(): void {
    this.restoreState();

    forkJoin({
      trainingCenters: this.lookups.getTrainingCenters(),
      promotions: this.lookups.getPromotions(),
    }).subscribe({
      next: ({ trainingCenters, promotions }) => {
        this.trainingCenters.set(trainingCenters);
        this.promotions.set(promotions);
        if (this.activeTrainingCenterPkid() === null && trainingCenters.length) {
          this.activeTrainingCenterPkid.set(trainingCenters[0].pkid);
        }
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法取得訓練中心／活動資料。' });
      },
    });
  }

  private restoreState(): void {
    const savedTc = sessionStorage.getItem(TC_KEY);
    if (savedTc) this.activeTrainingCenterPkid.set(Number(savedTc));

    const savedWeek = sessionStorage.getItem(WEEK_KEY);
    if (savedWeek) {
      const [y, m, d] = savedWeek.split('-').map(Number);
      if (y && m && d) this.weekStart.set(startOfWeekMonday(new Date(y, m - 1, d)));
    }
  }

  load(): void {
    const tc = this.activeTrainingCenterPkid();
    if (tc === null) return;

    this.loading.set(true);
    const days = this.days();
    this.service
      .query({
        trainingCenterPkid: tc,
        scheduleOnFrom: days[0].iso,
        scheduleOnTo: days[days.length - 1].iso,
      })
      .subscribe({
        next: (data) => {
          this.items.set(data);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法取得排程資料。' });
        },
      });
  }

  // ---- Tabs & week navigation ----

  selectTrainingCenter(pkid: number): void {
    if (pkid === this.activeTrainingCenterPkid()) return;
    this.activeTrainingCenterPkid.set(pkid);
    sessionStorage.setItem(TC_KEY, String(pkid));
    this.cancelEdit();
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
    sessionStorage.setItem(WEEK_KEY, toIsoDate(this.weekStart())!);
    this.cancelEdit();
    this.load();
  }

  weekRangeLabel(): string {
    const days = this.days();
    const from = days[0].date;
    const to = days[days.length - 1].date;
    return `${from.getMonth() + 1}/${from.getDate()} -- ${to.getMonth() + 1}/${to.getDate()}`;
  }

  // ---- Grid lookup ----

  itemFor(iso: string, slot: number): FeaturedPromoItem | undefined {
    return this.items().find((i) => i.scheduleOn === iso && i.slot === slot);
  }

  isEditing(iso: string, slot: number): boolean {
    const e = this.editing();
    return e !== null && e.scheduleOn === iso && e.slot === slot;
  }

  // ---- Inline edit / new / paste ----

  startEdit(iso: string, slot: number, item?: FeaturedPromoItem): void {
    this.editing.set({
      scheduleOn: iso,
      slot,
      pkid: item?.pkid ?? null,
      value: item
        ? {
            promotionPkid: item.promotionPkid,
            promoCode: item.promoCode ?? '',
            topic: item.topic,
            description: item.description,
          }
        : null,
    });
  }

  startPaste(iso: string, slot: number): void {
    const clip = this.clipboard();
    if (!clip) {
      this.messageService.add({ severity: 'warn', summary: '無可貼上內容', detail: '請先複製一筆資料。' });
      return;
    }
    this.editing.set({ scheduleOn: iso, slot, pkid: null, value: { ...clip } });
  }

  copy(item: FeaturedPromoItem): void {
    this.clipboard.set({
      promotionPkid: item.promotionPkid,
      promoCode: item.promoCode ?? '',
      topic: item.topic,
      description: item.description,
    });
    this.messageService.add({ severity: 'success', summary: '已複製', detail: `已複製「${item.promoCode}」。` });
  }

  cancelEdit(): void {
    this.editing.set(null);
  }

  onSave(value: FeaturedPromoItemFormValue): void {
    const ctx = this.editing();
    const tc = this.activeTrainingCenterPkid();
    if (!ctx || tc === null) return;

    const request: FeaturedPromoItemRequest = {
      pkid: ctx.pkid ?? 0,
      scheduleOn: ctx.scheduleOn,
      trainingCenterPkid: tc,
      slot: ctx.slot,
      promotionPkid: value.promotionPkid,
      topic: value.topic,
      description: value.description,
    };

    const op$ = ctx.pkid === null ? this.service.create(request) : this.service.update(request);
    op$.subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: '排程已儲存。' });
        this.cancelEdit();
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  // ---- Slot move (± ) ----

  moveSlotDown(item: FeaturedPromoItem): void {
    this.moveSlot(item, 1);
  }

  moveSlotUp(item: FeaturedPromoItem): void {
    this.moveSlot(item, -1);
  }

  /** Shift an item's slot up/down within 1–3, swapping with the neighbour already in the target slot. */
  private moveSlot(item: FeaturedPromoItem, delta: number): void {
    const target = item.slot + delta;
    const tc = this.activeTrainingCenterPkid();
    if (target < 1 || target > 3 || tc === null) return;

    const toRequest = (it: FeaturedPromoItem, slot: number): FeaturedPromoItemRequest => ({
      pkid: it.pkid,
      scheduleOn: it.scheduleOn,
      trainingCenterPkid: tc,
      slot,
      promotionPkid: it.promotionPkid,
      topic: it.topic,
      description: it.description,
    });

    const neighbour = this.itemFor(item.scheduleOn, target);
    const ops = [this.service.update(toRequest(item, target))];
    if (neighbour) ops.push(this.service.update(toRequest(neighbour, item.slot)));

    forkJoin(ops).subscribe({
      next: () => this.load(),
      error: () => {
        this.messageService.add({ severity: 'error', summary: '調整失敗', detail: '無法調整版位。' });
      },
    });
  }

  // ---- Delete ----

  confirmDelete(item: FeaturedPromoItem): void {
    this.confirmationService.confirm({
      header: '刪除確認',
      message: `確定要刪除版位 <b>${item.slot}</b>「${item.promoCode}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.delete(item),
    });
  }

  private delete(item: FeaturedPromoItem): void {
    this.service.delete(item.pkid).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: '已刪除', detail: '排程已刪除。' });
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除排程。' });
      },
    });
  }
}
