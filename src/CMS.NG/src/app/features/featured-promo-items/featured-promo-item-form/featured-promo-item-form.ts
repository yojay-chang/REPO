import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { InputTextModule } from 'primeng/inputtext';

import { PromotionLookup } from '@core/models/promotion.model';

/** Resolved values emitted on Save — the PromoCode has already been looked up to a promotionPkid. */
export interface FeaturedPromoItemFormValue {
  promotionPkid: number;
  promoCode: string;
  topic: string;
  description: string;
}

/**
 * Inline Edit / New form for a single scheduler slot (spec ui-update / ui-new). The user enters a
 * Promotion2 PromoCode; on Save it is looked up against {@link promotions} to resolve the
 * Promotion_pkid before emitting. Reused for New (empty) and Paste (pre-filled from the clipboard).
 */
@Component({
  selector: 'app-featured-promo-item-form',
  imports: [ReactiveFormsModule, AutoCompleteModule, InputTextModule],
  templateUrl: './featured-promo-item-form.html',
  styleUrl: './featured-promo-item-form.scss',
})
export class FeaturedPromoItemForm implements OnInit {
  private readonly fb = inject(FormBuilder);

  /** Full PromoCode lookup list — resolves the entered PromoCode → Promotion_pkid. */
  @Input() promotions: PromotionLookup[] = [];
  /** Initial values for Edit / Paste; null for a blank New. */
  @Input() value: FeaturedPromoItemFormValue | null = null;

  @Output() readonly save = new EventEmitter<FeaturedPromoItemFormValue>();
  @Output() readonly cancel = new EventEmitter<void>();

  readonly suggestions = signal<PromotionLookup[]>([]);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    promoCode: this.fb.nonNullable.control('', [Validators.required]),
    topic: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    description: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(300)]),
  });

  ngOnInit(): void {
    if (this.value) {
      this.form.patchValue({
        promoCode: this.value.promoCode,
        topic: this.value.topic,
        description: this.value.description,
      });
    }
  }

  /** p-autocomplete completeMethod — filter the PromoCode list by the typed query. */
  complete(event: AutoCompleteCompleteEvent): void {
    const q = (event.query ?? '').toLowerCase();
    this.suggestions.set(this.promotions.filter((p) => p.promoCode.toLowerCase().includes(q)));
  }

  onSubmit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    // p-autocomplete may hand back the selected object; normalize to the PromoCode string.
    const selected = raw.promoCode as string | PromotionLookup;
    const promoCode = typeof selected === 'string' ? selected.trim() : selected.promoCode;

    const promotion = this.promotions.find((p) => p.promoCode === promoCode);
    if (!promotion) {
      this.errorMessage.set('找不到對應的 PromoCode，請從清單選擇。');
      return;
    }

    this.save.emit({
      promotionPkid: promotion.pkid,
      promoCode: promotion.promoCode,
      topic: raw.topic.trim(),
      description: raw.description.trim(),
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
