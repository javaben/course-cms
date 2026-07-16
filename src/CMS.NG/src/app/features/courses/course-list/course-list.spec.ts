import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CourseList } from './course-list';
import { Course } from '@core/models/course.model';

const base = environment.apiBaseUrl;

function makeRow(): Course {
  return {
    pkid: 1,
    title: 'Azure 基礎',
    officialTitle: null,
    courseId: 'AZ-900',
    prodCourseId: 'P-AZ900',
    friendlyUrl: 'azure-900',
    displayOrder: 2,
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
    certificationPkids: [],
    jobCategoryPkids: [],
  };
}

// The full record the update flow fetches (has the N-N lists the list row lacks).
function fullCourse(): Course {
  return { ...makeRow(), certificationPkids: [10, 11], jobCategoryPkids: [7] };
}

describe('CourseList', () => {
  let fixture: ComponentFixture<CourseList>;
  let component: CourseList;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [CourseList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        ConfirmationService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseList);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function init(rows: Course[] = [makeRow()]): void {
    fixture.detectChanges();
    http.expectOne(`${base}/api/lookups/partners`).flush([{ pkid: 1, name: '微軟', label: '微軟' }]);
    http.expectOne(`${base}/api/lookups/course-groups`).flush([{ pkid: 5, description: '雲端', label: '雲端' }]);
    http.expectOne(`${base}/api/lookups/publish-statuses`).flush([
      { pkid: 1, description: '草稿', label: '1 - 草稿' },
      { pkid: 2, description: '已發布', label: '2 - 已發布' },
    ]);
    http.expectOne(`${base}/api/courses/query`).flush(rows);
    fixture.detectChanges();
  }

  it('loads courses through the query endpoint', () => {
    init();
    expect(component.courses().length).toBe(1);
    expect(component.courses()[0].title).toBe('Azure 基礎');
  });

  // ---- Activation: double-click only --------------------------------

  it('enters edit mode on double-click but not on single-click', () => {
    init();
    const cell: HTMLElement = fixture.nativeElement.querySelector('td[data-field="title"]');

    cell.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
    expect(component.editing()).toBeNull();
    expect(cell.querySelector('input')).toBeNull();

    cell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    fixture.detectChanges();
    expect(component.editing()).toEqual({ pkid: 1, field: 'title' });
    expect(cell.querySelector('input')).not.toBeNull();
  });

  // ---- Read-only columns --------------------------------------------

  it('treats pkid and the FK columns as read-only', () => {
    init();
    expect(component.isEditable('pkid')).toBeFalse();
    expect(component.isEditable('partnerPkid')).toBeFalse();
    expect(component.isEditable('courseGroupPkid')).toBeFalse();
    expect(component.isEditable('title')).toBeTrue();

    // startEdit is a no-op for a non-editable field...
    component.startEdit(makeRow(), 'pkid');
    expect(component.editing()).toBeNull();

    // ...and the FK cell has no double-click editor wired up.
    const partnerCell: HTMLElement = fixture.nativeElement.querySelector('td[data-field="partner"]');
    partnerCell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    fixture.detectChanges();
    expect(component.editing()).toBeNull();
  });

  // ---- Persist on blur ----------------------------------------------

  it('persists on blur via the update endpoint, preserving the N-N lists', () => {
    init();
    const row = component.courses()[0];

    component.startEdit(row, 'title');
    component.editValue = 'Azure 進階';
    component.commit(row, 'title'); // blur handler

    // First it fetches the full record (to keep the junctions), then PUTs.
    http.expectOne(`${base}/api/courses/1`).flush(fullCourse());
    const put = http.expectOne(`${base}/api/courses`);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.title).toBe('Azure 進階');
    expect(put.request.body.certificationPkids).toEqual([10, 11]); // preserved, not wiped
    put.flush(null);

    expect(component.editing()).toBeNull();
    expect(component.courses()[0].title).toBe('Azure 進階');
  });

  // ---- 上架狀態 dropdown (the only p-select inline editor) ------------

  it('persists a changed 上架狀態 via the update endpoint', () => {
    init();
    const row = component.courses()[0]; // publishStatusPkid = 2

    component.startEdit(row, 'publishStatusPkid');
    component.editValue = 1; // p-select onChange writes the chosen pkid
    component.commit(row, 'publishStatusPkid');

    http.expectOne(`${base}/api/courses/1`).flush(fullCourse());
    const put = http.expectOne(`${base}/api/courses`);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.publishStatusPkid).toBe(1);
    put.flush(null);

    expect(component.editing()).toBeNull();
    expect(component.courses()[0].publishStatusPkid).toBe(1);
    expect(component.courses()[0].publishStatus?.label).toBe('1 - 草稿'); // refreshed label
  });

  it('keeps the 上架狀態 editor open when its dropdown blurs (no premature commit)', () => {
    init();
    const cell: HTMLElement = fixture.nativeElement.querySelector('td[data-field="publishStatusPkid"]');

    cell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    fixture.detectChanges();
    expect(component.editing()).toEqual({ pkid: 1, field: 'publishStatusPkid' });

    // Opening the overlay blurs the trigger; that must NOT commit and close the cell.
    const select: HTMLElement = cell.querySelector('p-select')!;
    select.dispatchEvent(new FocusEvent('blur', { bubbles: true }));
    fixture.detectChanges();

    expect(component.editing()).toEqual({ pkid: 1, field: 'publishStatusPkid' }); // still editing
    http.expectNone(`${base}/api/courses/1`);
    http.expectNone(`${base}/api/courses`);
  });

  // ---- Validation ----------------------------------------------------

  it('blocks a cleared required field and stays in edit mode', () => {
    init();
    const row = component.courses()[0];

    component.startEdit(row, 'title');
    component.editValue = '   ';
    component.commit(row, 'title');

    expect(component.editError()).toBe('此欄位為必填。');
    expect(component.editing()).toEqual({ pkid: 1, field: 'title' }); // still editing
    http.expectNone(`${base}/api/courses/1`);
    http.expectNone(`${base}/api/courses`);
  });

  it('blocks a negative number', () => {
    init();
    const row = component.courses()[0];

    component.startEdit(row, 'hour');
    component.editValue = -3;
    component.commit(row, 'hour');

    expect(component.editError()).toBe('請輸入非負數字。');
    http.expectNone(`${base}/api/courses/1`);
  });

  it('blocks an invalid date', () => {
    init();
    const row = component.courses()[0];

    component.startEdit(row, 'scheduleOn');
    component.editValue = null; // not a Date
    component.commit(row, 'scheduleOn');

    expect(component.editError()).toBe('請輸入有效日期。');
    http.expectNone(`${base}/api/courses/1`);
  });

  it('blocks 上架日期 later than 下架日期', () => {
    init();
    const row = component.courses()[0]; // scheduleOff = 2036-03-01

    component.startEdit(row, 'scheduleOn');
    component.editValue = new Date(2037, 0, 1); // after scheduleOff
    component.commit(row, 'scheduleOn');

    expect(component.editError()).toBe('上架日期不可晚於下架日期。');
    http.expectNone(`${base}/api/courses/1`);
  });

  // ---- Revert on server failure -------------------------------------

  it('reverts the cell when the save fails', () => {
    init();
    const row = component.courses()[0];

    component.startEdit(row, 'title');
    component.editValue = '壞掉的標題';
    component.commit(row, 'title');

    http.expectOne(`${base}/api/courses/1`).flush(fullCourse());
    http.expectOne(`${base}/api/courses`).flush('boom', { status: 500, statusText: 'Server Error' });

    expect(component.editing()).toBeNull();                    // exited edit mode
    expect(component.courses()[0].title).toBe('Azure 基礎');    // reverted to original
  });

  // ---- Bulk actions (multi-select checkbox column) ------------------

  it('bulk-deletes the selected rows then reloads the list', () => {
    init([makeRow(), { ...makeRow(), pkid: 2, title: '第二課' }]);
    component.selectedCourses.set(component.courses()); // both rows ticked

    const confirm = TestBed.inject(ConfirmationService);
    spyOn(confirm, 'confirm').and.callFake((c) => {
      c.accept?.();
      return confirm;
    });

    component.confirmBulkDelete();

    const d1 = http.expectOne(`${base}/api/courses/1`);
    const d2 = http.expectOne(`${base}/api/courses/2`);
    expect(d1.request.method).toBe('DELETE');
    expect(d2.request.method).toBe('DELETE');
    d1.flush(null);
    d2.flush(null);

    http.expectOne(`${base}/api/courses/query`).flush([]); // list reloaded
    expect(component.selectedCourses().length).toBe(0);
  });

  it('does nothing when bulk delete is invoked with no selection', () => {
    init();
    component.selectedCourses.set([]);
    component.confirmBulkDelete();
    http.expectNone(`${base}/api/courses/1`);
  });

  it('exports the selected rows to a CSV download', () => {
    init();
    component.selectedCourses.set([component.courses()[0]]);

    const createUrl = spyOn(URL, 'createObjectURL').and.returnValue('blob:fake');
    spyOn(URL, 'revokeObjectURL');
    const clickSpy = spyOn(HTMLAnchorElement.prototype, 'click');

    component.exportSelected();

    expect(createUrl).toHaveBeenCalledTimes(1);
    expect(createUrl.calls.mostRecent().args[0] instanceof Blob).toBeTrue();
    expect(clickSpy).toHaveBeenCalledTimes(1);
  });
});
