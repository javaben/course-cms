import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { CourseGroupService } from './course-group.service';
import { CourseGroupRequest } from '@core/models/course-group.model';

describe('CourseGroupService', () => {
  let service: CourseGroupService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/course-groups`;

  const sample: CourseGroupRequest = { pkid: 3, description: '資料科學' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseGroupService);
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
    service.query({ keyword: '技術' }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '技術' });
    req.flush([]);
  });

  it('getById GETs by numeric pkid', () => {
    service.getById(1).subscribe();
    const req = http.expectOne(`${base}/1`);
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

  it('delete DELETEs by pkid', () => {
    service.delete(3).subscribe();
    const req = http.expectOne(`${base}/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
