import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CourseDetail } from './course-detail';

const base = environment.apiBaseUrl;

const course = {
  pkid: 1,
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
  partner: { pkid: 1, name: '微軟', label: '微軟' },
  courseGroup: null,
  publishStatus: { pkid: 2, description: '已發布', label: '2 - 已發布' },
  certificationPkids: [10],
  jobCategoryPkids: [7],
};

describe('CourseDetail', () => {
  let fixture: ComponentFixture<CourseDetail>;
  let component: CourseDetail;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseDetail);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flushLoad(): void {
    fixture.detectChanges();
    http.expectOne(`${base}/api/courses/1`).flush(course);
    http.expectOne(`${base}/api/lookups/certifications`).flush([
      { pkid: 10, label: '微軟 - Azure Fundamentals' },
      { pkid: 11, label: '甲骨文 - Oracle SQL' },
    ]);
    http.expectOne(`${base}/api/lookups/job-categories`).flush([
      { pkid: 7, description: '軟體開發', label: '軟體開發' },
    ]);
  }

  it('loads the course by pkid and resolves the FK label', () => {
    flushLoad();
    expect(component.course()?.title).toBe('Azure 基礎');
    expect(component.course()?.publishStatus?.label).toBe('2 - 已發布');
  });

  it('resolves the N-N id lists to labels', () => {
    flushLoad();
    expect(component.certificationLabels()).toEqual(['微軟 - Azure Fundamentals']);
    expect(component.jobCategoryLabels()).toEqual(['軟體開發']);
  });

  it('renders the QR code panel with the CourseId title', () => {
    flushLoad();
    fixture.detectChanges();
    const qrTitle: HTMLElement = fixture.nativeElement.querySelector('app-course-qr-code .qr-title');
    expect(qrTitle.textContent?.trim()).toBe('AZ-900');
  });
});
