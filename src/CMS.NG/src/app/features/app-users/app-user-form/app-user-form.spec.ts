import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { AppUserForm } from './app-user-form';
import { AuthService } from '@core/services/auth.service';

const ROLES_URL = `${environment.apiBaseUrl}/api/lookups/app-roles`;
const userUrl = (id: string) => `${environment.apiBaseUrl}/api/app-users/${id}`;
const RESET_BUTTON_LABEL = '重設為預設密碼';

function tokenWithRoles(roles: string[]): string {
  const b64 = (o: unknown) =>
    btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64({ role: roles })}.sig`;
}

function seedSession(roles: string[]): void {
  sessionStorage.setItem(
    AuthService.STORAGE_KEY,
    JSON.stringify({ userId: 'admin', userName: 'Admin', accessToken: tokenWithRoles(roles) }),
  );
}

function loadEditUser(http: HttpTestingController): void {
  http.expectOne(ROLES_URL).flush([]);
  http.expectOne(userUrl('helen')).flush({
    pkid: 1,
    userId: 'helen',
    userName: 'Helen Chen',
    isActive: true,
    passwordUpdatedTime: null,
    roleCount: 0,
    roleIds: [],
  });
}

function configure(id: string | null): {
  fixture: ComponentFixture<AppUserForm>;
  component: AppUserForm;
  http: HttpTestingController;
} {
  TestBed.configureTestingModule({
    imports: [AppUserForm],
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
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
      },
    ],
  });
  const fixture = TestBed.createComponent(AppUserForm);
  return {
    fixture,
    component: fixture.componentInstance,
    http: TestBed.inject(HttpTestingController),
  };
}

describe('AppUserForm', () => {
  beforeEach(() => sessionStorage.clear());

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('starts in add mode with active default and invalid form', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();
    http.expectOne(ROLES_URL).flush([]);

    expect(component.isEdit()).toBeFalse();
    expect(component.form.controls.isActive.value).toBeTrue();
    expect(component.form.controls.userId.disabled).toBeFalse();
    expect(component.form.valid).toBeFalse(); // userId & userName empty
  });

  it('has no password control of any kind', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();
    http.expectOne(ROLES_URL).flush([]);

    const controls = Object.keys(component.form.controls);
    expect(controls).toEqual(['userId', 'userName', 'isActive', 'roleIds']);
    expect(controls.some((c) => c.toLowerCase().includes('password'))).toBeFalse();
  });

  it('loads the user and disables UserId in edit mode', () => {
    const { fixture, component, http } = configure('helen');
    fixture.detectChanges();

    http.expectOne(ROLES_URL).flush([
      { roleId: 'User', roleName: 'User', label: 'User (User)' },
    ]);
    http.expectOne(userUrl('helen')).flush({
      pkid: 1,
      userId: 'helen',
      userName: 'Helen Chen',
      isActive: true,
      passwordUpdatedTime: null,
      roleCount: 1,
      roleIds: ['User'],
    });

    expect(component.isEdit()).toBeTrue();
    expect(component.form.controls.userId.disabled).toBeTrue();
    expect(component.form.controls.userName.value).toBe('Helen Chen');
    expect(component.form.getRawValue().roleIds).toEqual(['User']);
  });

  it('saves via POST when adding', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();
    http.expectOne(ROLES_URL).flush([]);

    component.form.patchValue({ userId: 'jenny', userName: 'Jenny Tsao' });
    component.save();

    const req = http.expectOne(`${environment.apiBaseUrl}/api/app-users`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.userId).toBe('jenny');
    expect('passwordHash' in req.request.body).toBeFalse();
    req.flush({});
  });

  it('shows the reset-password button for Admin users in edit mode', () => {
    seedSession(['Admin']);
    const { fixture, http } = configure('helen');
    fixture.detectChanges();
    loadEditUser(http);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(RESET_BUTTON_LABEL);
  });

  it('hides the reset-password button for non-Admin users', () => {
    seedSession(['User']);
    const { fixture, http } = configure('helen');
    fixture.detectChanges();
    loadEditUser(http);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain(RESET_BUTTON_LABEL);
  });
});
