import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { PartnerList } from './partner-list';
import { Partner } from '@core/models/partner.model';

const QUERY_URL = `${environment.apiBaseUrl}/api/partners/query`;

const microsoft: Partner = {
  pkid: 1,
  name: '微軟',
  appKey: 'MS',
  nameOnPartnerMenu: 'Microsoft 微軟',
  nameOnCourseDetailPage: '微軟',
  displayOrder: 2,
  imageFilename: 'ms.png',
};

describe('PartnerList', () => {
  let fixture: ComponentFixture<PartnerList>;
  let component: PartnerList;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [PartnerList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        ConfirmationService,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PartnerList);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);
    expect(component).toBeTruthy();
  });

  it('loads partners via POST /query on init', () => {
    fixture.detectChanges();
    const req = http.expectOne(QUERY_URL);
    expect(req.request.method).toBe('POST');
    req.flush([microsoft]);
    expect(component.partners().length).toBe(1);
    expect(component.partners()[0].name).toBe('微軟');
  });

  it('applyFilters persists filters and reloads', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.filters = { keyword: '微軟' };
    component.applyFilters();

    const req = http.expectOne(QUERY_URL);
    expect(req.request.body.keyword).toBe('微軟');
    req.flush([microsoft]);

    const saved = JSON.parse(sessionStorage.getItem('partner-list-filters')!);
    expect(saved.keyword).toBe('微軟');
    expect(component.drawerVisible()).toBeFalse();
  });

  it('onSort persists sort state', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.onSort({ field: 'displayOrder', order: -1 });

    const saved = JSON.parse(sessionStorage.getItem('partner-list-sort')!);
    expect(saved.sortField).toBe('displayOrder');
    expect(saved.sortOrder).toBe(-1);
  });
});
