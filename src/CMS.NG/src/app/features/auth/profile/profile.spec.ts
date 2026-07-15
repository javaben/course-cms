import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { Profile } from './profile';
import { AuthService } from '@core/services/auth.service';

const PROFILE_URL = `${environment.apiBaseUrl}/api/Auth/profile`;

function tokenWithRoles(roles: string[]): string {
  const b64 = (o: unknown) =>
    btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64({ role: roles })}.sig`;
}

describe('Profile', () => {
  let fixture: ComponentFixture<Profile>;
  let component: Profile;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    sessionStorage.setItem(
      AuthService.STORAGE_KEY,
      JSON.stringify({
        userId: 'helen',
        userName: 'Helen Chen',
        accessToken: tokenWithRoles(['Admin', 'User']),
      }),
    );

    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Profile);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('shows the UserId read-only', () => {
    const input = fixture.nativeElement.querySelector('#userId') as HTMLInputElement;
    expect(input.value).toBe('helen');
    expect(input.disabled).toBeTrue();
  });

  it('shows the roles read-only (display only, no role controls)', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Admin');
    expect(text).toContain('User');
    // Roles are not editable — there is no roles form control.
    expect(fixture.nativeElement.querySelector('[formControlName="roles"]')).toBeNull();
  });

  it('pre-fills the editable UserName', () => {
    expect(component.form.controls.userName.value).toBe('Helen Chen');
  });

  it('saving updates the shell name and session storage', () => {
    const auth = TestBed.inject(AuthService);
    component.form.controls.userName.setValue('Helen Renamed');

    component.save();

    const req = http.expectOne(PROFILE_URL);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ userName: 'Helen Renamed' });
    req.flush({ userId: 'helen', userName: 'Helen Renamed' });

    // Shell (signal) + session storage both reflect the new name.
    expect(auth.profile()!.userName).toBe('Helen Renamed');
    expect(JSON.parse(sessionStorage.getItem(AuthService.STORAGE_KEY)!).userName).toBe(
      'Helen Renamed',
    );
  });

  // ---- Change password: client validation ----------------------------

  const CHANGE_URL = `${environment.apiBaseUrl}/api/Auth/change-password`;

  it('does not submit and posts nothing when the new password fails complexity', () => {
    component.passwordForm.setValue({
      currentPassword: 'secret',
      newPassword: 'weak', // < 8, too few classes
      confirmNewPassword: 'weak',
    });

    component.changePassword();

    http.expectNone(CHANGE_URL);
    expect(component.passwordForm.controls.newPassword.hasError('complexity')).toBeTrue();
  });

  it('does not submit when confirm does not match', () => {
    component.passwordForm.setValue({
      currentPassword: 'secret',
      newPassword: 'NewPass#1',
      confirmNewPassword: 'Different#1',
    });

    component.changePassword();

    http.expectNone(CHANGE_URL);
    expect(component.passwordForm.hasError('mismatch')).toBeTrue();
  });

  it('posts the change when all client validation passes', () => {
    component.passwordForm.setValue({
      currentPassword: 'secret',
      newPassword: 'NewPass#1',
      confirmNewPassword: 'NewPass#1',
    });

    component.changePassword();

    const req = http.expectOne(CHANGE_URL);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      currentPassword: 'secret',
      newPassword: 'NewPass#1',
      confirmNewPassword: 'NewPass#1',
    });
    // No hash of any kind is sent.
    expect(JSON.stringify(req.request.body).toLowerCase()).not.toContain('hash');
    req.flush(null);
  });
});
