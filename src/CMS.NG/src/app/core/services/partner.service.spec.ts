import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { PartnerService } from './partner.service';
import { PartnerRequest } from '@core/models/partner.model';

describe('PartnerService', () => {
  let service: PartnerService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/partners`;

  const sample: PartnerRequest = {
    pkid: 3,
    name: '亞馬遜',
    appKey: 'AWS',
    nameOnPartnerMenu: 'AWS 亞馬遜',
    nameOnCourseDetailPage: '亞馬遜',
    displayOrder: 3,
    imageFilename: 'aws.png',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PartnerService);
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
    service.query({ keyword: '微軟' }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '微軟' });
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
