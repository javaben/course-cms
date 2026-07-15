import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { Login } from './login';
import { AuthService } from '@core/services/auth.service';

const LOGIN_URL = `${environment.apiBaseUrl}/api/Auth/login`;

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('does not submit when the form is empty', () => {
    component.submit();
    http.expectNone(LOGIN_URL);
    expect(component.form.invalid).toBeTrue();
  });

  it('posts credentials, stores the session, and navigates home on success', () => {
    const navigate = spyOn(router, 'navigateByUrl');
    component.form.setValue({ userId: 'helen', password: 'secret' });

    component.submit();

    const req = http.expectOne(LOGIN_URL);
    expect(req.request.body).toEqual({ userId: 'helen', password: 'secret' });
    req.flush({ userId: 'helen', userName: 'Helen', accessToken: 'tok' });

    expect(sessionStorage.getItem(AuthService.STORAGE_KEY)).not.toBeNull();
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('keeps the user on the page and does not store a session on 401', () => {
    const navigate = spyOn(router, 'navigateByUrl');
    component.form.setValue({ userId: 'helen', password: 'wrong' });

    component.submit();

    http.expectOne(LOGIN_URL).flush('bad', { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem(AuthService.STORAGE_KEY)).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });
});
