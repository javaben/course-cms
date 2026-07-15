import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { FeaturedPromoItemBoard } from './featured-promo-item-board';
import { FeaturedPromoItem } from '@core/models/featured-promo-item.model';
import { TrainingCenterLookup } from '@core/models/training-center-lookup.model';

const CENTERS_URL = `${environment.apiBaseUrl}/api/lookups/training-centers`;
const QUERY_URL = `${environment.apiBaseUrl}/api/featured-promo-items/query`;
const base = `${environment.apiBaseUrl}/api/featured-promo-items`;

const centers: TrainingCenterLookup[] = [
  { pkid: 1, name: '台北', label: '台北' },
  { pkid: 2, name: '新竹', label: '新竹' },
];

const item = (over: Partial<FeaturedPromoItem>): FeaturedPromoItem => ({
  pkid: 1,
  scheduleOn: '2026-03-16',
  trainingCenterPkid: 1,
  slot: 1,
  promotionPkid: 10,
  promoCode: '20251204_SkillTrainAI',
  topic: '成為能AI協作的程式設計師',
  description: '轉職就業養成班',
  ...over,
});

describe('FeaturedPromoItemBoard', () => {
  let fixture: ComponentFixture<FeaturedPromoItemBoard>;
  let component: FeaturedPromoItemBoard;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    sessionStorage.setItem('featured-promo-item-week', '2026-03-16'); // fixed Monday → deterministic

    await TestBed.configureTestingModule({
      imports: [FeaturedPromoItemBoard],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        ConfirmationService,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FeaturedPromoItemBoard);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** detectChanges → flush the training-center tabs → return the ensuing board query request. */
  function initAndFlushCenters() {
    fixture.detectChanges();
    http.expectOne(CENTERS_URL).flush(centers);
    return http.expectOne(QUERY_URL);
  }

  it('loads tabs then queries the default (first) center for the active week', () => {
    const query = initAndFlushCenters();
    expect(query.request.method).toBe('POST');
    expect(query.request.body).toEqual({ trainingCenterPkid: 1, weekStart: '2026-03-16' });
    query.flush([item({})]);

    expect(component.centers().length).toBe(2);
    expect(component.activeCenter()).toBe(1);
    expect(component.items().length).toBe(1);
  });

  it('builds a Monday..Sunday week for the grid', () => {
    initAndFlushCenters().flush([]);
    const days = component.days();
    expect(days.length).toBe(7);
    expect(days[0]).toBe('2026-03-16');
    expect(days[6]).toBe('2026-03-22');
    expect(component.weekLabel()).toBe('3/16 -- 3/22');
  });

  it('itemAt locates the row for a given day + slot', () => {
    initAndFlushCenters().flush([item({ pkid: 2, slot: 2, topic: 'slot two' })]);
    expect(component.itemAt('2026-03-16', 2)?.topic).toBe('slot two');
    expect(component.itemAt('2026-03-16', 1)).toBeNull();
  });

  it('selectCenter switches the tab and re-queries', () => {
    initAndFlushCenters().flush([]);

    component.selectCenter(2);
    const query = http.expectOne(QUERY_URL);
    expect(query.request.body.trainingCenterPkid).toBe(2);
    query.flush([]);
    expect(component.activeCenter()).toBe(2);
  });

  it('nextWeek advances the week by 7 days and re-queries', () => {
    initAndFlushCenters().flush([]);

    component.nextWeek();
    const query = http.expectOne(QUERY_URL);
    expect(query.request.body.weekStart).toBe('2026-03-23');
    query.flush([]);
    expect(component.weekLabel()).toBe('3/23 -- 3/29');
  });

  it('prevWeek goes back a week and re-queries', () => {
    initAndFlushCenters().flush([]);

    component.prevWeek();
    const query = http.expectOne(QUERY_URL);
    expect(query.request.body.weekStart).toBe('2026-03-09');
    query.flush([]);
  });

  it('copy buffers a cell and paste seeds an empty cell form', () => {
    initAndFlushCenters().flush([item({})]);

    component.copy(item({}));
    expect(component.copyBuffer()?.promoCode).toBe('20251204_SkillTrainAI');

    component.startPaste('2026-03-17', 3);
    const e = component.editing();
    expect(e?.scheduleOn).toBe('2026-03-17');
    expect(e?.slot).toBe(3);
    expect(e?.item).toBeNull();
    expect(e?.seed?.topic).toBe('成為能AI協作的程式設計師');
  });

  it('startPaste does nothing when nothing has been copied', () => {
    initAndFlushCenters().flush([]);
    component.startPaste('2026-03-17', 1);
    expect(component.editing()).toBeNull();
  });

  it('move POSTs the direction and reloads the board', () => {
    initAndFlushCenters().flush([item({ pkid: 7 })]);

    component.move(item({ pkid: 7 }), 1);
    const move = http.expectOne(`${base}/7/move`);
    expect(move.request.method).toBe('POST');
    expect(move.request.body).toEqual({ direction: 1 });
    move.flush(null);

    http.expectOne(QUERY_URL).flush([]); // reload after move
  });

  it('startEdit opens the inline form for a filled cell', () => {
    initAndFlushCenters().flush([]);
    const row = item({ pkid: 3 });
    component.startEdit('2026-03-16', 1, row);
    expect(component.isEditing('2026-03-16', 1)).toBeTrue();
    expect(component.editing()?.item?.pkid).toBe(3);
  });
});
