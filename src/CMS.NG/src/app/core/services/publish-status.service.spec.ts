import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { PublishStatusService } from './publish-status.service';
import { PublishStatusRequest } from '@core/models/publish-status.model';

describe('PublishStatusService', () => {
  let service: PublishStatusService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/publish-statuses`;

  const sample: PublishStatusRequest = {
    pkid: 4,
    description: '審核中',
    isDraft: true,
    isPublished: false,
    isDiscontinued: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PublishStatusService);
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
    service.query({ keyword: '發布' }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '發布' });
    req.flush([]);
  });

  it('getById GETs by numeric pkid', () => {
    service.getById(2).subscribe();
    const req = http.expectOne(`${base}/2`);
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
    service.delete(4).subscribe();
    const req = http.expectOne(`${base}/4`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
