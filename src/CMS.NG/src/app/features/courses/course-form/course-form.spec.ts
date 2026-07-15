import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CourseForm } from './course-form';

const base = environment.apiBaseUrl;

function configure(id: string | null): void {
  TestBed.configureTestingModule({
    imports: [CourseForm],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      provideNoopAnimations(),
      providePrimeNG({ theme: { preset: Aura } }),
      MessageService,
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
      },
    ],
  });
}

/** Asserts the action toolbar is pinned (sticky) and still holds Save + Cancel. */
function expectStickyToolbar(fixture: ComponentFixture<CourseForm>): void {
  const header: HTMLElement = fixture.nativeElement.querySelector('.page-header');
  expect(header).withContext('.page-header toolbar renders').toBeTruthy();
  expect(getComputedStyle(header).position).toBe('sticky');

  const labels = Array.from(header.querySelectorAll('button')).map((b) => b.textContent?.trim() ?? '');
  expect(labels.some((t) => t.includes('儲存'))).withContext('Save button present').toBeTrue();
  expect(labels.some((t) => t.includes('取消'))).withContext('Cancel button present').toBeTrue();
}

function flushLookups(http: HttpTestingController): void {
  http.expectOne(`${base}/api/lookups/partners`).flush([{ pkid: 1, name: '微軟', label: '微軟' }]);
  http.expectOne(`${base}/api/lookups/course-groups`).flush([{ pkid: 5, description: '雲端', label: '雲端' }]);
  http.expectOne(`${base}/api/lookups/publish-statuses`).flush([{ pkid: 2, description: '已發布', label: '2 - 已發布' }]);
  http.expectOne(`${base}/api/lookups/certifications`).flush([{ pkid: 10, label: '微軟 - Azure' }]);
  http.expectOne(`${base}/api/lookups/job-categories`).flush([{ pkid: 7, description: '軟體開發', label: '軟體開發' }]);
}

describe('CourseForm', () => {
  let fixture: ComponentFixture<CourseForm>;
  let component: CourseForm;
  let http: HttpTestingController;

  describe('new', () => {
    beforeEach(async () => {
      configure(null);
      await TestBed.compileComponents();
      fixture = TestBed.createComponent(CourseForm);
      component = fixture.componentInstance;
      http = TestBed.inject(HttpTestingController);
      fixture.detectChanges();
      flushLookups(http);
    });

    afterEach(() => http.verify());

    it('starts in create mode with an empty pkid', () => {
      expect(component.isEdit()).toBeFalse();
      expect(component.pkid()).toBe(0);
    });

    it('pins the Save/Cancel toolbar to the top (sticky) on the New form', () => {
      expectStickyToolbar(fixture);
    });

    it('does not submit an invalid form', () => {
      component.save();
      http.expectNone(`${base}/api/courses`);
      expect(component.form.controls.title.touched).toBeTrue();
    });

    it('defaults 下架日期 to 上架日期 + 10 years when 上架日期 changes', () => {
      component.form.controls.scheduleOn.setValue(new Date(2026, 2, 1)); // 2026-03-01
      const off = component.form.controls.scheduleOff.value!;
      expect(off.getFullYear()).toBe(2036);
      expect(off.getMonth()).toBe(2);
      expect(off.getDate()).toBe(1);
    });

    it('POSTs a create with locally-serialized dates and the N-N id lists', () => {
      component.form.patchValue({
        title: 'Azure 基礎',
        courseId: 'AZ-900',
        prodCourseId: 'P-AZ900',
        friendlyUrl: 'azure-900',
        displayOrder: 1,
        partnerPkid: 1,
        publishStatusPkid: 2,
        hour: 14,
        listPrice: 12000,
        learningCredit: 3.5,
        certificationPkids: [10],
        jobCategoryPkids: [7],
      });
      component.form.controls.scheduleOn.setValue(new Date(2026, 2, 1)); // also sets scheduleOff +10y

      component.save();

      const req = http.expectOne(`${base}/api/courses`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.scheduleOn).toBe('2026-03-01');
      expect(req.request.body.scheduleOff).toBe('2036-03-01');
      expect(req.request.body.certificationPkids).toEqual([10]);
      expect(req.request.body.jobCategoryPkids).toEqual([7]);
      req.flush({ ...req.request.body, pkid: 9 });
    });
  });

  describe('edit', () => {
    beforeEach(async () => {
      configure('1');
      await TestBed.compileComponents();
      fixture = TestBed.createComponent(CourseForm);
      component = fixture.componentInstance;
      http = TestBed.inject(HttpTestingController);
      fixture.detectChanges();
      flushLookups(http);
      http.expectOne(`${base}/api/courses/1`).flush({
        pkid: 1,
        title: 'Azure 基礎',
        officialTitle: null,
        courseId: 'AZ-900',
        prodCourseId: 'P-AZ900',
        friendlyUrl: 'azure-900',
        displayOrder: 1,
        partnerPkid: 1,
        courseGroupPkid: 5,
        publishStatusPkid: 2,
        scheduleOn: '2026-03-01',
        scheduleOff: '2030-06-30',
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
        certificationPkids: [10],
        jobCategoryPkids: [7],
      });
    });

    afterEach(() => http.verify());

    it('pins the Save/Cancel toolbar to the top (sticky) on the Edit form', () => {
      expectStickyToolbar(fixture);
    });

    it('loads the course and keeps the stored 下架日期 (not the +10y default)', () => {
      expect(component.isEdit()).toBeTrue();
      expect(component.pkid()).toBe(1);
      expect(component.form.controls.title.value).toBe('Azure 基礎');
      const off = component.form.controls.scheduleOff.value!;
      expect(off.getFullYear()).toBe(2030); // stored value wins over the auto-default
      expect(off.getMonth()).toBe(5);
      expect(off.getDate()).toBe(30);
    });

    it('PUTs an update carrying the pkid', () => {
      component.form.controls.title.setValue('Azure 進階');
      component.save();
      const req = http.expectOne(`${base}/api/courses`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body.pkid).toBe(1);
      expect(req.request.body.title).toBe('Azure 進階');
      req.flush(null);
    });
  });
});
