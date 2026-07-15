import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';

import { FeaturedPromoItemList } from './featured-promo-item-list';
import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { FeaturedPromoItem } from '@core/models/featured-promo-item.model';

const ITEMS = [
  {
    pkid: 1, scheduleOn: '2026-03-16', trainingCenterPkid: 1, slot: 1, promotionPkid: 1,
    topic: '成為能AI協作的程式設計師', description: '轉職就業養成班', promoCode: '20251204_SkillTrainAI',
    trainingCenterName: '台北',
  },
  {
    pkid: 2, scheduleOn: '2026-03-16', trainingCenterPkid: 1, slot: 2, promotionPkid: 2,
    topic: 'Google AI工具一次掌握', description: '不需技術基礎', promoCode: '251211_GoogleAI',
    trainingCenterName: '台北',
  },
] as FeaturedPromoItem[];

describe('FeaturedPromoItemList', () => {
  let serviceSpy: jasmine.SpyObj<FeaturedPromoItemService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;

  function setup() {
    serviceSpy = jasmine.createSpyObj<FeaturedPromoItemService>('FeaturedPromoItemService', [
      'query', 'create', 'update', 'delete',
    ]);
    serviceSpy.query.and.returnValue(of(ITEMS));
    serviceSpy.create.and.returnValue(of(ITEMS[0]));
    serviceSpy.update.and.returnValue(of(ITEMS[0]));
    serviceSpy.delete.and.returnValue(of(void 0));

    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getTrainingCenters', 'getPromotions']);
    lookupSpy.getTrainingCenters.and.returnValue(
      of([{ pkid: 1, name: '台北' }, { pkid: 2, name: '新竹' }]),
    );
    lookupSpy.getPromotions.and.returnValue(
      of([{ pkid: 1, promoCode: '20251204_SkillTrainAI' }, { pkid: 2, promoCode: '251211_GoogleAI' }]),
    );

    TestBed.configureTestingModule({
      imports: [FeaturedPromoItemList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        ConfirmationService,
        { provide: FeaturedPromoItemService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
      ],
    });

    const fixture = TestBed.createComponent(FeaturedPromoItemList);
    // Pin the week to the seeded Monday so the ScheduleOn range is deterministic.
    fixture.componentInstance.weekStart.set(new Date(2026, 2, 16)); // 2026-03-16 (Mon)
    return fixture;
  }

  beforeEach(() => sessionStorage.clear());

  it('loads lookups, defaults to the first TrainingCenter tab, and queries the week', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(lookupSpy.getTrainingCenters).toHaveBeenCalled();
    expect(cmp.trainingCenters().length).toBe(2);
    expect(cmp.activeTrainingCenterPkid()).toBe(1);
    expect(serviceSpy.query).toHaveBeenCalledWith({
      trainingCenterPkid: 1,
      scheduleOnFrom: '2026-03-16',
      scheduleOnTo: '2026-03-22',
    });
    expect(cmp.items().length).toBe(2);
  });

  it('builds seven Monday→Sunday day columns with the week label', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(cmp.days().length).toBe(7);
    expect(cmp.days()[0].iso).toBe('2026-03-16');
    expect(cmp.days()[6].iso).toBe('2026-03-22');
    expect(cmp.weekRangeLabel()).toBe('3/16 -- 3/22');
  });

  it('selectTrainingCenter switches the active tab and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.selectTrainingCenter(2);

    expect(cmp.activeTrainingCenterPkid()).toBe(2);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(serviceSpy.query.calls.mostRecent().args[0].trainingCenterPkid).toBe(2);
  });

  it('nextWeek shifts the range forward one week and re-queries', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.nextWeek();

    expect(cmp.days()[0].iso).toBe('2026-03-23');
    expect(serviceSpy.query.calls.mostRecent().args[0]).toEqual({
      trainingCenterPkid: 1,
      scheduleOnFrom: '2026-03-23',
      scheduleOnTo: '2026-03-29',
    });
  });

  it('itemFor finds the item occupying a given day + slot', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    expect(cmp.itemFor('2026-03-16', 2)?.pkid).toBe(2);
    expect(cmp.itemFor('2026-03-16', 3)).toBeUndefined();
  });

  it('copy stores the row on the clipboard; startPaste opens the editor pre-filled', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.copy(ITEMS[0]);
    expect(cmp.clipboard()?.promotionPkid).toBe(1);

    cmp.startPaste('2026-03-17', 3);
    const editing = cmp.editing();
    expect(editing).not.toBeNull();
    expect(editing!.pkid).toBeNull();
    expect(editing!.scheduleOn).toBe('2026-03-17');
    expect(editing!.value?.promoCode).toBe('20251204_SkillTrainAI');
  });

  it('onSave creates a new item for an empty slot', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.startEdit('2026-03-18', 1);
    cmp.onSave({ promotionPkid: 2, promoCode: '251211_GoogleAI', topic: 'T', description: 'D' });

    expect(serviceSpy.create).toHaveBeenCalledWith(
      jasmine.objectContaining({ pkid: 0, scheduleOn: '2026-03-18', trainingCenterPkid: 1, slot: 1, promotionPkid: 2 }),
    );
    expect(cmp.editing()).toBeNull();
  });

  it('onSave updates an existing item when editing a filled slot', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.startEdit('2026-03-16', 1, ITEMS[0]);
    cmp.onSave({ promotionPkid: 2, promoCode: '251211_GoogleAI', topic: 'T2', description: 'D2' });

    expect(serviceSpy.update).toHaveBeenCalledWith(
      jasmine.objectContaining({ pkid: 1, slot: 1, promotionPkid: 2, topic: 'T2' }),
    );
  });

  it('moveSlotDown swaps with the neighbour occupying the target slot', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    // slot 1 (pkid 1) moves down to slot 2, which is occupied by pkid 2 → swap (two updates).
    cmp.moveSlotDown(ITEMS[0]);

    expect(serviceSpy.update).toHaveBeenCalledWith(jasmine.objectContaining({ pkid: 1, slot: 2 }));
    expect(serviceSpy.update).toHaveBeenCalledWith(jasmine.objectContaining({ pkid: 2, slot: 1 }));
  });

  it('confirmDelete deletes when the confirmation is accepted', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;
    const confirmation = TestBed.inject(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });

    cmp.confirmDelete(ITEMS[0]);

    expect(serviceSpy.delete).toHaveBeenCalledWith(1);
  });

  it('surfaces an error toast when the query fails', () => {
    const fixture = setup();
    fixture.detectChanges();
    const cmp = fixture.componentInstance;
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    const messages = TestBed.inject(MessageService);
    const addSpy = spyOn(messages, 'add');

    cmp.load();

    expect(addSpy).toHaveBeenCalledWith(jasmine.objectContaining({ severity: 'error' }));
  });
});
