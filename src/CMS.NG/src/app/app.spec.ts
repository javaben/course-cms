import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';

import { App } from './app';
import { AuthService } from '@core/services/auth.service';

// Unsigned JWT with the given roles in the `role` claim.
function tokenWithRoles(roles: string[]): string {
  const b64 = (o: unknown) =>
    btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64({ role: roles })}.sig`;
}

function seedSession(userName: string, roles: string[]): void {
  sessionStorage.setItem(
    AuthService.STORAGE_KEY,
    JSON.stringify({ userId: 'helen', userName, accessToken: tokenWithRoles(roles) }),
  );
}

describe('App shell', () => {
  let fixture: ComponentFixture<App>;

  function createShell(): HTMLElement {
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        ConfirmationService,
      ],
    });
  });

  it('shows the 系統管理 Admin menu when the roles include Admin', () => {
    seedSession('Helen', ['Admin', 'User']);

    const el = createShell();

    expect(el.textContent).toContain('系統管理');
  });

  it('hides the 系統管理 Admin menu when the roles do NOT include Admin', () => {
    seedSession('Bob', ['User']);

    const el = createShell();

    expect(el.textContent).not.toContain('系統管理');
    // ...but non-admin groups still render.
    expect(el.textContent).toContain('課程管理');
  });

  it('shows the signed-in user name and logs out', () => {
    seedSession('Helen Chen', ['User']);

    const el = createShell();
    expect(el.textContent).toContain('Helen Chen');

    fixture.componentInstance.logout();
    expect(sessionStorage.getItem(AuthService.STORAGE_KEY)).toBeNull();
  });
});
