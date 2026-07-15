import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

import { FeaturedPromoItemForm } from './featured-promo-item-form';
import { PromotionLookup } from '@core/models/promotion.model';

const PROMOTIONS: PromotionLookup[] = [
  { pkid: 1, promoCode: '20251204_SkillTrainAI' },
  { pkid: 2, promoCode: '251211_GoogleAI' },
];

describe('FeaturedPromoItemForm', () => {
  function setup(value: FeaturedPromoItemForm['value'] = null) {
    TestBed.configureTestingModule({
      imports: [FeaturedPromoItemForm],
      providers: [provideNoopAnimations()],
    });
    const fixture = TestBed.createComponent(FeaturedPromoItemForm);
    fixture.componentInstance.promotions = PROMOTIONS;
    fixture.componentInstance.value = value;
    fixture.detectChanges();
    return fixture;
  }

  it('does not emit save when the form is empty (required fields)', () => {
    const fixture = setup();
    const cmp = fixture.componentInstance;
    const saveSpy = jasmine.createSpy('save');
    cmp.save.subscribe(saveSpy);

    cmp.onSubmit();

    expect(saveSpy).not.toHaveBeenCalled();
  });

  it('resolves the entered PromoCode to a promotionPkid and emits it', () => {
    const fixture = setup();
    const cmp = fixture.componentInstance;
    const saveSpy = jasmine.createSpy('save');
    cmp.save.subscribe(saveSpy);

    cmp.form.setValue({
      promoCode: '251211_GoogleAI',
      topic: 'Google AI工具一次掌握',
      description: '不需技術基礎！',
    });
    cmp.onSubmit();

    expect(saveSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({ promotionPkid: 2, promoCode: '251211_GoogleAI', topic: 'Google AI工具一次掌握' }),
    );
    expect(cmp.errorMessage()).toBeNull();
  });

  it('shows an error and does not emit when the PromoCode is unknown', () => {
    const fixture = setup();
    const cmp = fixture.componentInstance;
    const saveSpy = jasmine.createSpy('save');
    cmp.save.subscribe(saveSpy);

    cmp.form.setValue({ promoCode: 'UNKNOWN_CODE', topic: 'x', description: 'y' });
    cmp.onSubmit();

    expect(saveSpy).not.toHaveBeenCalled();
    expect(cmp.errorMessage()).not.toBeNull();
  });

  it('patches initial values for Edit / Paste', () => {
    const fixture = setup({
      promotionPkid: 1,
      promoCode: '20251204_SkillTrainAI',
      topic: '成為能AI協作的程式設計師',
      description: '轉職就業養成班',
    });
    const cmp = fixture.componentInstance;

    expect(cmp.form.controls.promoCode.value).toBe('20251204_SkillTrainAI');
    expect(cmp.form.controls.topic.value).toBe('成為能AI協作的程式設計師');
  });

  it('filters PromoCode suggestions by the typed query', () => {
    const fixture = setup();
    const cmp = fixture.componentInstance;

    cmp.complete({ query: 'google', originalEvent: new Event('input') });

    expect(cmp.suggestions().length).toBe(1);
    expect(cmp.suggestions()[0].pkid).toBe(2);
  });

  it('emits cancel', () => {
    const fixture = setup();
    const cmp = fixture.componentInstance;
    const cancelSpy = jasmine.createSpy('cancel');
    cmp.cancel.subscribe(cancelSpy);

    cmp.onCancel();

    expect(cancelSpy).toHaveBeenCalled();
  });
});
