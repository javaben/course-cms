import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { PublishStatusList } from './publish-status-list';
import { PublishStatus } from '@core/models/publish-status.model';

const QUERY_URL = `${environment.apiBaseUrl}/api/publish-statuses/query`;

const published: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

describe('PublishStatusList', () => {
  let fixture: ComponentFixture<PublishStatusList>;
  let component: PublishStatusList;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [PublishStatusList],
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

    fixture = TestBed.createComponent(PublishStatusList);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);
    expect(component).toBeTruthy();
  });

  it('loads statuses via POST /query on init', () => {
    fixture.detectChanges();
    const req = http.expectOne(QUERY_URL);
    expect(req.request.method).toBe('POST');
    req.flush([published]);
    expect(component.statuses().length).toBe(1);
    expect(component.statuses()[0].description).toBe('已發布');
  });

  it('applyFilters persists filters and reloads', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.filters = { keyword: '發布', isDraft: null, isPublished: true, isDiscontinued: null };
    component.applyFilters();

    const req = http.expectOne(QUERY_URL);
    expect(req.request.body.keyword).toBe('發布');
    expect(req.request.body.isPublished).toBeTrue();
    req.flush([published]);

    const saved = JSON.parse(sessionStorage.getItem('publish-status-list-filters')!);
    expect(saved.keyword).toBe('發布');
    expect(component.drawerVisible()).toBeFalse();
  });

  it('onSort persists sort state', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.onSort({ field: 'description', order: -1 });

    const saved = JSON.parse(sessionStorage.getItem('publish-status-list-sort')!);
    expect(saved.sortField).toBe('description');
    expect(saved.sortOrder).toBe(-1);
  });
});
