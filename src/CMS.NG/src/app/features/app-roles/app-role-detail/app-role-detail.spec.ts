import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { AppRoleDetail } from './app-role-detail';

const USERS_URL = `${environment.apiBaseUrl}/api/lookups/app-users`;
const ROLE_URL = `${environment.apiBaseUrl}/api/app-roles/Admin`;

describe('AppRoleDetail', () => {
  let fixture: ComponentFixture<AppRoleDetail>;
  let component: AppRoleDetail;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppRoleDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'Admin' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppRoleDetail);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the role and maps assigned user labels', () => {
    fixture.detectChanges();

    http.expectOne(ROLE_URL).flush({
      pkid: 1,
      roleId: 'Admin',
      roleName: 'Administrator',
      permissionLevel: 1,
      description: '系統管理員',
      userCount: 2,
      userIds: ['helen', 'miles'],
    });
    http.expectOne(USERS_URL).flush([
      { userId: 'helen', userName: 'helen', label: 'helen (helen)' },
      { userId: 'miles', userName: 'Miles Sun', label: 'Miles Sun (miles@uuu.com.tw)' },
    ]);

    expect(component.role()?.roleName).toBe('Administrator');
    expect(component.userLabels()).toEqual(['helen (helen)', 'Miles Sun (miles@uuu.com.tw)']);
  });
});
