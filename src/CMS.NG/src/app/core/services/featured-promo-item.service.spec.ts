import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { FeaturedPromoItemService } from './featured-promo-item.service';
import { FeaturedPromoItemRequest } from '@core/models/featured-promo-item.model';

describe('FeaturedPromoItemService', () => {
  let service: FeaturedPromoItemService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/featured-promo-items`;

  const sample: FeaturedPromoItemRequest = {
    pkid: 5,
    scheduleOn: '2026-03-16',
    trainingCenterPkid: 1,
    slot: 2,
    promotionPkid: 11,
    topic: 'Google AI工具一次掌握',
    description: '不需技術基礎',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeaturedPromoItemService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('query POSTs the filter to /query', () => {
    service.query({ trainingCenterPkid: 1, weekStart: '2026-03-16' }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ trainingCenterPkid: 1, weekStart: '2026-03-16' });
    req.flush([]);
  });

  it('getById GETs by numeric pkid', () => {
    service.getById(5).subscribe();
    const req = http.expectOne(`${base}/5`);
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
    service.delete(5).subscribe();
    const req = http.expectOne(`${base}/5`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('move POSTs the direction to /{id}/move', () => {
    service.move(5, 1).subscribe();
    const req = http.expectOne(`${base}/5/move`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ direction: 1 });
    req.flush(null);
  });
});
