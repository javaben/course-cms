import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { AppUserDetail } from './app-user-detail';

const ROLES_URL = `${environment.apiBaseUrl}/api/lookups/app-roles`;
const USER_URL = `${environment.apiBaseUrl}/api/app-users/helen`;

describe('AppUserDetail', () => {
  let fixture: ComponentFixture<AppUserDetail>;
  let component: AppUserDetail;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppUserDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        ConfirmationService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'helen' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppUserDetail);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the user and maps assigned role labels', () => {
    fixture.detectChanges();

    http.expectOne(USER_URL).flush({
      pkid: 1,
      userId: 'helen',
      userName: 'Helen Chen',
      isActive: true,
      passwordUpdatedTime: null,
      roleCount: 2,
      roleIds: ['Admin', 'User'],
    });
    http.expectOne(ROLES_URL).flush([
      { roleId: 'Admin', roleName: 'Administrator', label: 'Administrator (Admin)' },
      { roleId: 'User', roleName: 'User', label: 'User (User)' },
    ]);

    expect(component.user()?.userName).toBe('Helen Chen');
    expect(component.roleLabels()).toEqual(['Administrator (Admin)', 'User (User)']);
  });
});
