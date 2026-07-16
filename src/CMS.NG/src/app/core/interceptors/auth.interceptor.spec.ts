import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { environment } from '@env/environment';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from '@core/services/auth.service';

const URL = `${environment.apiBaseUrl}/api/app-roles`;
const LOGIN_URL = `${environment.apiBaseUrl}/api/Auth/login`;

function seedSession(accessToken: string): void {
  sessionStorage.setItem(
    AuthService.STORAGE_KEY,
    JSON.stringify({ userId: 'helen', userName: 'Helen', accessToken }),
  );
}

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let router: { navigateByUrl: jasmine.Spy };
  let messages: { add: jasmine.Spy };

  beforeEach(() => {
    sessionStorage.clear();
    router = { navigateByUrl: jasmine.createSpy('navigateByUrl') };
    messages = { add: jasmine.createSpy('add') };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: MessageService, useValue: messages },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('attaches the Bearer header when a token is in session storage', () => {
    seedSession('tok123'); // seed BEFORE the first request so AuthService reads it
    const client = TestBed.inject(HttpClient);

    client.get(URL).subscribe();

    const req = http.expectOne(URL);
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok123');
    req.flush([]);
  });

  it('sends no Authorization header when there is no token', () => {
    const client = TestBed.inject(HttpClient);

    client.get(URL).subscribe();

    const req = http.expectOne(URL);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('on a 401 clears session storage and redirects to /login', () => {
    seedSession('tok123');
    const client = TestBed.inject(HttpClient);

    client.get(URL).subscribe({ next: () => {}, error: () => {} });

    http.expectOne(URL).flush('nope', { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem(AuthService.STORAGE_KEY)).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('does NOT redirect on a 401 from the login endpoint (login page handles it)', () => {
    const client = TestBed.inject(HttpClient);

    client.post(LOGIN_URL, {}).subscribe({ next: () => {}, error: () => {} });

    http.expectOne(LOGIN_URL).flush('bad', { status: 401, statusText: 'Unauthorized' });

    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('on a 500 shows a friendly error toast using the safe message from the body', () => {
    seedSession('tok123');
    const client = TestBed.inject(HttpClient);

    client.get(URL).subscribe({ next: () => {}, error: () => {} });

    http.expectOne(URL).flush(
      { message: 'An unexpected error occurred.' },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(messages.add).toHaveBeenCalledTimes(1);
    const arg = messages.add.calls.mostRecent().args[0];
    expect(arg.severity).toBe('error');
    expect(arg.detail).toBe('An unexpected error occurred.');
    // A server error is not a 401 → no session clear / redirect.
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not toast on a 400 validation error (forms surface it themselves)', () => {
    seedSession('tok123');
    const client = TestBed.inject(HttpClient);

    client.get(URL).subscribe({ next: () => {}, error: () => {} });

    http.expectOne(URL).flush(
      { errors: { name: ['required'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(messages.add).not.toHaveBeenCalled();
  });
});
