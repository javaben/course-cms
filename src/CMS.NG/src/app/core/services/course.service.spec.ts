import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';

import { CourseService } from './course.service';
import { CourseRequest } from '@core/models/course.model';

describe('CourseService', () => {
  let service: CourseService;
  let http: HttpTestingController;
  const base = `${environment.apiBaseUrl}/api/courses`;

  const sample: CourseRequest = {
    pkid: 5,
    title: 'Azure 基礎',
    officialTitle: null,
    courseId: 'AZ-900',
    prodCourseId: 'P-AZ900',
    friendlyUrl: 'azure-900',
    displayOrder: 1,
    partnerPkid: 1,
    courseGroupPkid: null,
    publishStatusPkid: 2,
    scheduleOn: '2026-03-01',
    scheduleOff: '2036-03-01',
    hour: 14,
    listPrice: 12000,
    learningCredit: 3.5,
    material: null,
    objective: null,
    target: null,
    prerequisites: null,
    outline: null,
    towardCertOrExam: null,
    note: null,
    otherInfo: null,
    canRepeat: true,
    certificationPkids: [10, 11],
    jobCategoryPkids: [7],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseService);
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
    service.query({ keyword: 'Azure', partnerPkid: 1 }).subscribe();
    const req = http.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'Azure', partnerPkid: 1 });
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

  it('downloadPdf GETs the pdf route as a blob (so the bearer token attaches)', () => {
    service.downloadPdf(5).subscribe();
    const req = http.expectOne(`${base}/5/pdf`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['%PDF-1.7'], { type: 'application/pdf' }));
  });
});
