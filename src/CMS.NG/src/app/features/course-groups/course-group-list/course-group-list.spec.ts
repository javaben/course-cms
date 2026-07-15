import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CourseGroupList } from './course-group-list';
import { CourseGroup } from '@core/models/course-group.model';

const QUERY_URL = `${environment.apiBaseUrl}/api/course-groups/query`;

const infotech: CourseGroup = { pkid: 1, description: '資訊技術' };

describe('CourseGroupList', () => {
  let fixture: ComponentFixture<CourseGroupList>;
  let component: CourseGroupList;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [CourseGroupList],
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

    fixture = TestBed.createComponent(CourseGroupList);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);
    expect(component).toBeTruthy();
  });

  it('loads groups via POST /query on init', () => {
    fixture.detectChanges();
    const req = http.expectOne(QUERY_URL);
    expect(req.request.method).toBe('POST');
    req.flush([infotech]);
    expect(component.groups().length).toBe(1);
    expect(component.groups()[0].description).toBe('資訊技術');
  });

  it('applyFilters persists filters and reloads', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.filters = { keyword: '技術' };
    component.applyFilters();

    const req = http.expectOne(QUERY_URL);
    expect(req.request.body.keyword).toBe('技術');
    req.flush([infotech]);

    const saved = JSON.parse(sessionStorage.getItem('course-group-list-filters')!);
    expect(saved.keyword).toBe('技術');
    expect(component.drawerVisible()).toBeFalse();
  });

  it('onSort persists sort state', () => {
    fixture.detectChanges();
    http.expectOne(QUERY_URL).flush([]);

    component.onSort({ field: 'description', order: -1 });

    const saved = JSON.parse(sessionStorage.getItem('course-group-list-sort')!);
    expect(saved.sortField).toBe('description');
    expect(saved.sortOrder).toBe(-1);
  });
});
