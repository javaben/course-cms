import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { AppRoleForm } from './app-role-form';

const USERS_URL = `${environment.apiBaseUrl}/api/lookups/app-users`;
const roleUrl = (id: string) => `${environment.apiBaseUrl}/api/app-roles/${id}`;

function configure(id: string | null): {
  fixture: ComponentFixture<AppRoleForm>;
  component: AppRoleForm;
  http: HttpTestingController;
} {
  TestBed.configureTestingModule({
    imports: [AppRoleForm],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      provideNoopAnimations(),
      providePrimeNG({ theme: { preset: Aura } }),
      MessageService,
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
      },
    ],
  });
  const fixture = TestBed.createComponent(AppRoleForm);
  return {
    fixture,
    component: fixture.componentInstance,
    http: TestBed.inject(HttpTestingController),
  };
}

describe('AppRoleForm', () => {
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('starts in add mode with default permission level and invalid form', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();
    http.expectOne(USERS_URL).flush([]);

    expect(component.isEdit()).toBeFalse();
    expect(component.form.controls.permissionLevel.value).toBe(100);
    expect(component.form.controls.roleId.disabled).toBeFalse();
    expect(component.form.valid).toBeFalse(); // roleId & roleName empty
  });

  it('loads the role and disables RoleId in edit mode', () => {
    const { fixture, component, http } = configure('Admin');
    fixture.detectChanges();

    http.expectOne(USERS_URL).flush([
      { userId: 'helen', userName: 'helen', label: 'helen (helen)' },
    ]);
    http.expectOne(roleUrl('Admin')).flush({
      pkid: 1,
      roleId: 'Admin',
      roleName: 'Administrator',
      permissionLevel: 1,
      description: '系統管理員',
      userCount: 1,
      userIds: ['helen'],
    });

    expect(component.isEdit()).toBeTrue();
    expect(component.form.controls.roleId.disabled).toBeTrue();
    expect(component.form.controls.roleName.value).toBe('Administrator');
    expect(component.form.getRawValue().userIds).toEqual(['helen']);
  });

  it('saves via POST when adding', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();
    http.expectOne(USERS_URL).flush([]);

    component.form.patchValue({ roleId: 'Editor', roleName: 'Editor', permissionLevel: 50 });
    component.save();

    const req = http.expectOne(`${environment.apiBaseUrl}/api/app-roles`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.roleId).toBe('Editor');
    req.flush({});
  });
});
