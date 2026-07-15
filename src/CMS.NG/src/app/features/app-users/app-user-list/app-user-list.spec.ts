import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { AppUserList } from './app-user-list';
import { AppUser } from '@core/models/app-user.model';

const QUERY_URL = `${environment.apiBaseUrl}/api/app-users/query`;

const helen: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Chen',
  isActive: true,
  passwordUpdatedTime: null,
  roleCount: 2,
  roleIds: [],
};

describe('AppUserList', () => {
  let fixture: ComponentFixture<AppUserList>;
  let component: AppUserList;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AppUserList],
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

    fixture = TestBed.createComponent(AppUserList);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);
    expect(component).toBeTruthy();
  });

  it('loads users via POST /query on init', () => {
    fixture.detectChanges();
    const req = http.expectOne(QUERY_URL);
    expect(req.request.method).toBe('POST');
    req.flush([helen]);
    expect(component.users().length).toBe(1);
    expect(component.users()[0].userId).toBe('helen');
  });

  it('applyFilters persists filters (incl. isActive) and reloads', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.filters = { keyword: 'helen', isActive: true };
    component.applyFilters();

    const req = http.expectOne(QUERY_URL);
    expect(req.request.body.keyword).toBe('helen');
    expect(req.request.body.isActive).toBeTrue();
    req.flush([helen]);

    const saved = JSON.parse(sessionStorage.getItem('app-user-list-filters')!);
    expect(saved.keyword).toBe('helen');
    expect(saved.isActive).toBeTrue();
    expect(component.drawerVisible()).toBeFalse();
  });

  it('onSort persists sort state', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.onSort({ field: 'userName', order: -1 });

    const saved = JSON.parse(sessionStorage.getItem('app-user-list-sort')!);
    expect(saved.sortField).toBe('userName');
    expect(saved.sortOrder).toBe(-1);
  });
});
