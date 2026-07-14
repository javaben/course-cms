import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { AppRoleList } from './app-role-list';
import { AppRole } from '@core/models/app-role.model';

const QUERY_URL = `${environment.apiBaseUrl}/api/app-roles/query`;

const admin: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 3,
  userIds: [],
};

describe('AppRoleList', () => {
  let fixture: ComponentFixture<AppRoleList>;
  let component: AppRoleList;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AppRoleList],
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

    fixture = TestBed.createComponent(AppRoleList);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);
    expect(component).toBeTruthy();
  });

  it('loads roles via POST /query on init', () => {
    fixture.detectChanges();
    const req = http.expectOne(QUERY_URL);
    expect(req.request.method).toBe('POST');
    req.flush([admin]);
    expect(component.roles().length).toBe(1);
    expect(component.roles()[0].roleId).toBe('Admin');
  });

  it('applyFilters persists filters and reloads', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.filters = { keyword: 'admin', permissionLevel: null };
    component.applyFilters();

    const req = http.expectOne(QUERY_URL);
    expect(req.request.body.keyword).toBe('admin');
    req.flush([admin]);

    const saved = JSON.parse(sessionStorage.getItem('app-role-list-filters')!);
    expect(saved.keyword).toBe('admin');
    expect(component.drawerVisible()).toBeFalse();
  });

  it('onSort persists sort state', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.onSort({ field: 'roleName', order: -1 });

    const saved = JSON.parse(sessionStorage.getItem('app-role-list-sort')!);
    expect(saved.sortField).toBe('roleName');
    expect(saved.sortOrder).toBe(-1);
  });
});
