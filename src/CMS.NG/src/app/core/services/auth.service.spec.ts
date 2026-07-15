import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { AuthService } from './auth.service';

const LOGIN_URL = `${environment.apiBaseUrl}/api/Auth/login`;
const PROFILE_URL = `${environment.apiBaseUrl}/api/Auth/profile`;

// Builds an unsigned JWT string with the given payload (only the payload segment is read).
function jwt(payload: Record<string, unknown>): string {
  const b64 = (o: unknown) =>
    btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64(payload)}.sig`;
}

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login POSTs credentials and stores the profile in session storage', () => {
    const profile = { userId: 'helen', userName: 'Helen', accessToken: jwt({ role: ['Admin'] }) };

    service.login({ userId: 'helen', password: 'secret' }).subscribe();
    const req = http.expectOne(LOGIN_URL);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'helen', password: 'secret' });
    req.flush(profile);

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.token).toBe(profile.accessToken);
    const stored = JSON.parse(sessionStorage.getItem(AuthService.STORAGE_KEY)!);
    expect(stored.userId).toBe('helen');
  });

  it('decodes roles from the token and reports Admin membership', () => {
    service.login({ userId: 'helen', password: 'x' }).subscribe();
    http.expectOne(LOGIN_URL).flush({
      userId: 'helen',
      userName: 'Helen',
      accessToken: jwt({ role: ['Admin', 'User'] }),
    });

    expect(service.roles()).toEqual(['Admin', 'User']);
    expect(service.hasRole('Admin')).toBeTrue();
    expect(service.isAdmin()).toBeTrue();
  });

  it('treats a single-string role claim as one role and non-admins as not admin', () => {
    service.login({ userId: 'bob', password: 'x' }).subscribe();
    http.expectOne(LOGIN_URL).flush({
      userId: 'bob',
      userName: 'Bob',
      accessToken: jwt({ role: 'User' }),
    });

    expect(service.roles()).toEqual(['User']);
    expect(service.isAdmin()).toBeFalse();
  });

  it('updateUserName PUTs and refreshes the stored/canonical name', () => {
    service.login({ userId: 'helen', password: 'x' }).subscribe();
    http.expectOne(LOGIN_URL).flush({ userId: 'helen', userName: 'Helen', accessToken: 'tok' });

    service.updateUserName('  Helen R.  ').subscribe();
    const req = http.expectOne(PROFILE_URL);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ userName: '  Helen R.  ' });
    req.flush({ userId: 'helen', userName: 'Helen R.' }); // server returns trimmed

    expect(service.profile()!.userName).toBe('Helen R.');
    expect(JSON.parse(sessionStorage.getItem(AuthService.STORAGE_KEY)!).userName).toBe('Helen R.');
  });

  it('logout clears session storage', () => {
    service.login({ userId: 'helen', password: 'x' }).subscribe();
    http.expectOne(LOGIN_URL).flush({ userId: 'helen', userName: 'Helen', accessToken: 'tok' });

    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
    expect(sessionStorage.getItem(AuthService.STORAGE_KEY)).toBeNull();
  });
});
