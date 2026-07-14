import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { AppRoleService } from './app-role.service';
import { AppRoleRequest } from '@core/models/app-role.model';

describe('AppRoleService', () => {
  let service: AppRoleService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/app-roles`;

  const sample: AppRoleRequest = {
    roleId: 'Editor',
    roleName: 'Editor',
    permissionLevel: 50,
    description: '編輯者',
    userIds: ['helen'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppRoleService);
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
    service.query({ keyword: 'admin' }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'admin' });
    req.flush([]);
  });

  it('getById URL-encodes the id', () => {
    service.getById('a b/c').subscribe();
    const req = http.expectOne(`${base}/a%20b%2Fc`);
    expect(req.request.method).toBe('GET');
    req.flush({} as never);
  });

  it('create POSTs the request body', () => {
    service.create(sample).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(sample);
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
    service.delete('Editor').subscribe();
    const req = http.expectOne(`${base}/Editor`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
