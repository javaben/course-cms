import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthService } from '@core/services/auth.service';

describe('authGuard', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  const run = () =>
    TestBed.runInInjectionContext(() => authGuard(null as never, null as never));

  it('redirects to /login when there is no token', () => {
    const result = run();

    expect(result).not.toBe(true);
    const router = TestBed.inject(Router);
    expect((result as UrlTree).toString()).toBe(router.parseUrl('/login').toString());
  });

  it('allows activation when a token is present', () => {
    sessionStorage.setItem(
      AuthService.STORAGE_KEY,
      JSON.stringify({ userId: 'helen', userName: 'Helen', accessToken: 'tok' }),
    );

    expect(run()).toBe(true);
  });
});
