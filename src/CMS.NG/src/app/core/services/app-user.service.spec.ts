import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { AppUserService } from './app-user.service';
import { AppUserRequest } from '@core/models/app-user.model';

describe('AppUserService', () => {
  let service: AppUserService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/app-users`;

  const sample: AppUserRequest = {
    userId: 'jenny',
    userName: 'Jenny Tsao',
    isActive: true,
    roleIds: ['User'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppUserService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getAll GETs the collection', () => {
    service.getAll().subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('query POSTs the filter to /query', () => {
    service.query({ keyword: 'helen', isActive: true }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'helen', isActive: true });
    req.flush([]);
  });

  it('getById URL-encodes the id', () => {
    service.getById('a b/c').subscribe();
    const req = http.expectOne(`${base}/a%20b%2Fc`);
    expect(req.request.method).toBe('GET');
    req.flush({} as never);
  });

  it('create POSTs the request body (no password field)', () => {
    service.create(sample).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sample);
    expect('passwordHash' in req.request.body).toBeFalse();
    req.flush({} as never);
  });

  it('update PUTs the request body (no id in route)', () => {
    service.update(sample).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(sample);
    req.flush(null);
  });

  it('delete DELETEs by encoded id', () => {
    service.delete('jenny').subscribe();
    const req = http.expectOne(`${base}/jenny`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('resetPassword POSTs to the encoded reset-password route', () => {
    service.resetPassword('a b').subscribe();
    const req = http.expectOne(`${base}/a%20b/reset-password`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });
});
