import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CellContext, FeaturedPromoItemForm } from './featured-promo-item-form';
import { FeaturedPromoItem } from '@core/models/featured-promo-item.model';
import { PromotionLookup } from '@core/models/promotion-lookup.model';

const base = environment.apiBaseUrl;
const ITEMS_URL = `${base}/api/featured-promo-items`;
const promoUrl = (code: string) => `${base}/api/lookups/promotions/${encodeURIComponent(code)}`;

const context: CellContext = { scheduleOn: '2026-03-16', trainingCenterPkid: 1, slot: 2 };

const promo: PromotionLookup = {
  pkid: 11,
  promoCode: '251211_GoogleAI',
  topic: 'Google AI工具一次掌握',
  description: '不需技術基礎',
};

const existing: FeaturedPromoItem = {
  pkid: 9,
  scheduleOn: '2026-03-16',
  trainingCenterPkid: 1,
  slot: 2,
  promotionPkid: 10,
  promoCode: '20251204_SkillTrainAI',
  topic: '舊標題',
  description: '舊說明',
};

describe('FeaturedPromoItemForm', () => {
  let fixture: ComponentFixture<FeaturedPromoItemForm>;
  let component: FeaturedPromoItemForm;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturedPromoItemForm],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FeaturedPromoItemForm);
    component = fixture.componentInstance;
    component.context = context;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // ---- New -----------------------------------------------------------

  it('starts empty for a new cell', () => {
    fixture.detectChanges();
    expect(component.isEdit).toBeFalse();
    expect(component.form.getRawValue()).toEqual({ promoCode: '', topic: '', description: '' });
  });

  it('resolves the PromoCode on blur and pre-fills empty Topic/Description', () => {
    fixture.detectChanges();
    component.form.controls.promoCode.setValue('251211_GoogleAI');

    component.onPromoCodeBlur();
    http.expectOne(promoUrl('251211_GoogleAI')).flush(promo);

    expect(component.form.controls.topic.value).toBe('Google AI工具一次掌握');
    expect(component.form.controls.description.value).toBe('不需技術基礎');
    expect(component.lookupError()).toBeNull();
  });

  it('save resolves the code then POSTs a create, emitting saved', () => {
    fixture.detectChanges();
    const saved = jasmine.createSpy('saved');
    component.saved.subscribe(saved);

    component.form.setValue({
      promoCode: '251211_GoogleAI',
      topic: 'Google AI工具一次掌握',
      description: '不需技術基礎',
    });
    component.save();

    http.expectOne(promoUrl('251211_GoogleAI')).flush(promo); // authoritative resolve

    const create = http.expectOne(ITEMS_URL);
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      pkid: 0,
      scheduleOn: '2026-03-16',
      trainingCenterPkid: 1,
      slot: 2,
      promotionPkid: 11, // resolved FK
      topic: 'Google AI工具一次掌握',
      description: '不需技術基礎',
    });
    create.flush({});

    expect(saved).toHaveBeenCalled();
  });

  it('does not submit when the form is invalid', () => {
    fixture.detectChanges();
    component.save(); // promoCode/topic/description all empty
    http.expectNone(promoUrl(''));
    http.expectNone(ITEMS_URL);
    expect(component.form.controls.promoCode.touched).toBeTrue(); // markAllAsTouched ran
  });

  it('surfaces a lookup error and does not persist when the code is unknown', () => {
    fixture.detectChanges();
    component.form.setValue({ promoCode: 'nope', topic: 't', description: 'd' });
    component.save();

    http.expectOne(promoUrl('nope')).flush('not found', { status: 404, statusText: 'Not Found' });

    expect(component.lookupError()).toBe('找不到此活動代碼。');
    http.expectNone(ITEMS_URL);
    expect(component.saving()).toBeFalse();
  });

  // ---- Edit ----------------------------------------------------------

  it('patches existing values in edit mode', () => {
    component.item = existing;
    fixture.detectChanges();
    expect(component.isEdit).toBeTrue();
    expect(component.form.getRawValue()).toEqual({
      promoCode: '20251204_SkillTrainAI',
      topic: '舊標題',
      description: '舊說明',
    });
  });

  it('save in edit mode PUTs an update carrying the existing pkid', () => {
    component.item = existing;
    fixture.detectChanges();
    const saved = jasmine.createSpy('saved');
    component.saved.subscribe(saved);

    component.form.controls.topic.setValue('新標題');
    component.save();

    http.expectOne(promoUrl('20251204_SkillTrainAI')).flush({ ...promo, pkid: 10, promoCode: '20251204_SkillTrainAI' });

    const update = http.expectOne(ITEMS_URL);
    expect(update.request.method).toBe('PUT');
    expect(update.request.body.pkid).toBe(9);
    expect(update.request.body.topic).toBe('新標題');
    expect(update.request.body.promotionPkid).toBe(10);
    update.flush(null);

    expect(saved).toHaveBeenCalled();
  });

  it('seeds a pasted new cell from the copied values', () => {
    component.seed = { promoCode: '251211_GoogleAI', topic: '貼上標題', description: '貼上說明' };
    fixture.detectChanges();
    expect(component.isEdit).toBeFalse();
    expect(component.form.getRawValue()).toEqual({
      promoCode: '251211_GoogleAI',
      topic: '貼上標題',
      description: '貼上說明',
    });
  });

  it('cancel emits cancelled', () => {
    fixture.detectChanges();
    const cancelled = jasmine.createSpy('cancelled');
    component.cancelled.subscribe(cancelled);
    component.cancel();
    expect(cancelled).toHaveBeenCalled();
  });
});
