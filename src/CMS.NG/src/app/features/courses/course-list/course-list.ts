import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';

import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course, CourseQuery, CourseRequest } from '@core/models/course.model';
import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';
import { fromIso, toIso } from '../course-form/course-date.util';

const FILTERS_KEY = 'course-list-filters';
const SORT_KEY = 'course-list-sort';
const PAGE_KEY = 'course-list-page';

/** Columns that support inline editing — everything except pkid + the two FK-lookup columns. */
export type EditableField =
  | 'displayOrder'
  | 'courseId'
  | 'prodCourseId'
  | 'title'
  | 'publishStatusPkid'
  | 'scheduleOn'
  | 'scheduleOff'
  | 'hour'
  | 'listPrice'
  | 'learningCredit'
  | 'canRepeat';

const EDITABLE_FIELDS: readonly EditableField[] = [
  'displayOrder',
  'courseId',
  'prodCourseId',
  'title',
  'publishStatusPkid',
  'scheduleOn',
  'scheduleOff',
  'hour',
  'listPrice',
  'learningCredit',
  'canRepeat',
];

interface EditingCell {
  pkid: number;
  field: EditableField;
}
interface SortState {
  sortField: string | null;
  sortOrder: number;
}
interface PageState {
  first: number;
  rows: number;
}

interface FilterModel {
  keyword: string | null;
  partnerPkid: number | null;
  courseGroupPkid: number | null;
  publishStatusPkid: number | null;
  scheduleOnFrom: Date | null;
  scheduleOnTo: Date | null;
  scheduleOffFrom: Date | null;
  scheduleOffTo: Date | null;
  canRepeat: boolean | null;
}

function emptyFilters(): FilterModel {
  return {
    keyword: null,
    partnerPkid: null,
    courseGroupPkid: null,
    publishStatusPkid: null,
    scheduleOnFrom: null,
    scheduleOnTo: null,
    scheduleOffFrom: null,
    scheduleOffTo: null,
    canRepeat: null,
  };
}

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    DatePickerModule,
    CheckboxModule,
    TagModule,
    TooltipModule,
  ],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss',
})
export class CourseList implements OnInit {
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  readonly courses = signal<Course[]>([]);
  readonly loading = signal(false);
  readonly drawerVisible = signal(false);

  readonly partners = signal<PartnerLookup[]>([]);
  readonly courseGroups = signal<CourseGroupLookup[]>([]);
  readonly publishStatuses = signal<PublishStatusLookup[]>([]);
  readonly canRepeatOptions = [
    { label: '全部', value: null },
    { label: '是', value: true },
    { label: '否', value: false },
  ];

  // --- Inline editing state -------------------------------------------
  readonly editing = signal<EditingCell | null>(null);
  readonly editError = signal<string | null>(null);
  /** Working value bound to the active cell editor (ngModel). */
  editValue: unknown = null;
  private saving = false;

  get drawerVisibleModel(): boolean {
    return this.drawerVisible();
  }
  set drawerVisibleModel(value: boolean) {
    this.drawerVisible.set(value);
  }

  filters: FilterModel = emptyFilters();

  sortState: SortState = { sortField: null, sortOrder: 1 };
  pageState: PageState = { first: 0, rows: 20 };
  readonly rowsPerPageOptions = [10, 20, 50];

  ngOnInit(): void {
    this.restoreState();

    const incoming = this.route.snapshot.queryParamMap.get('partnerPkid');
    if (incoming != null) this.filters.partnerPkid = Number(incoming);

    forkJoin({
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
    }).subscribe({
      next: ({ partners, courseGroups, publishStatuses }) => {
        this.partners.set(partners);
        this.courseGroups.set(courseGroups);
        this.publishStatuses.set(publishStatuses);
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得搜尋選項。' }),
    });

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service
      .query(this.toQuery())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (rows) => this.courses.set(rows),
        error: () =>
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程資料。' }),
      });
  }

  private toQuery(): CourseQuery {
    const f = this.filters;
    return {
      keyword: f.keyword,
      partnerPkid: f.partnerPkid,
      courseGroupPkid: f.courseGroupPkid,
      publishStatusPkid: f.publishStatusPkid,
      scheduleOnFrom: toIso(f.scheduleOnFrom),
      scheduleOnTo: toIso(f.scheduleOnTo),
      scheduleOffFrom: toIso(f.scheduleOffFrom),
      scheduleOffTo: toIso(f.scheduleOffTo),
      canRepeat: f.canRepeat,
    };
  }

  // ==================================================================
  //  Inline editing
  // ==================================================================

  /** Whether a column supports inline editing (pkid + FK-lookup columns do not). */
  isEditable(field: string): field is EditableField {
    return (EDITABLE_FIELDS as readonly string[]).includes(field);
  }

  isEditing(course: Course, field: string): boolean {
    const e = this.editing();
    return e != null && e.pkid === course.pkid && e.field === field;
  }

  /** Enter edit mode for a cell (double-click). No-op for read-only columns. */
  startEdit(course: Course, field: string): void {
    if (!this.isEditable(field) || this.saving) return;
    this.editError.set(null);
    this.editValue = this.toEditModel(course, field);
    this.editing.set({ pkid: course.pkid, field });
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.editError.set(null);
  }

  /** Commit the active cell (on editor blur / change). Validates, then persists via the update endpoint. */
  commit(course: Course, field: EditableField): void {
    if (this.saving || !this.isEditing(course, field)) return;

    const value = this.editValue;
    const error = this.validate(course, field, value);
    if (error) {
      this.editError.set(error); // keep the cell in edit mode so the user can fix it
      return;
    }
    this.editError.set(null);
    this.saving = true;

    // Fetch the full record first so the two N-N id lists are preserved on update.
    this.service.getById(course.pkid).subscribe({
      next: (full) => {
        const request = this.toRequest(full);
        this.applyField(request, field, value);
        this.service.update(request).subscribe({
          next: () => {
            this.saving = false;
            this.applyToRow(course, field, value);
            this.editing.set(null);
            this.messages.add({ severity: 'success', summary: '已更新', detail: `「${course.title}」已更新。` });
          },
          error: () => {
            this.saving = false;
            this.editing.set(null); // revert: the row object was never mutated
            this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '更新失敗，已還原變更。' });
          },
        });
      },
      error: () => {
        this.saving = false;
        this.editing.set(null);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程資料，已還原。' });
      },
    });
  }

  private validate(course: Course, field: EditableField, value: unknown): string | null {
    switch (field) {
      case 'title':
      case 'courseId':
      case 'prodCourseId':
        return typeof value === 'string' && value.trim().length > 0 ? null : '此欄位為必填。';
      case 'displayOrder':
      case 'hour':
      case 'listPrice':
      case 'learningCredit':
        return typeof value === 'number' && !isNaN(value) && value >= 0 ? null : '請輸入非負數字。';
      case 'publishStatusPkid':
        return value != null ? null : '請選擇上架狀態。';
      case 'scheduleOn': {
        if (!(value instanceof Date) || isNaN(value.getTime())) return '請輸入有效日期。';
        const off = fromIso(course.scheduleOff);
        return off && value > off ? '上架日期不可晚於下架日期。' : null;
      }
      case 'scheduleOff': {
        if (!(value instanceof Date) || isNaN(value.getTime())) return '請輸入有效日期。';
        const on = fromIso(course.scheduleOn);
        return on && on > value ? '下架日期不可早於上架日期。' : null;
      }
      case 'canRepeat':
        return null;
    }
  }

  private toEditModel(course: Course, field: EditableField): unknown {
    if (field === 'scheduleOn' || field === 'scheduleOff') return fromIso(course[field]);
    return course[field];
  }

  private applyField(request: CourseRequest, field: EditableField, value: unknown): void {
    if (field === 'scheduleOn' || field === 'scheduleOff') {
      request[field] = toIso(value as Date) ?? request[field];
    } else {
      (request as unknown as Record<string, unknown>)[field] = value;
    }
  }

  /** Update the on-screen row after a successful save (dates → iso, publishStatus → refreshed label). */
  private applyToRow(course: Course, field: EditableField, value: unknown): void {
    const updated: Course = { ...course };
    if (field === 'scheduleOn' || field === 'scheduleOff') {
      updated[field] = toIso(value as Date) ?? course[field];
    } else if (field === 'publishStatusPkid') {
      updated.publishStatusPkid = value as number;
      const s = this.publishStatuses().find((x) => x.pkid === value);
      if (s) updated.publishStatus = { pkid: s.pkid, description: s.description, label: s.label };
    } else {
      (updated as unknown as Record<string, unknown>)[field] = value;
    }
    this.courses.update((rows) => rows.map((r) => (r.pkid === course.pkid ? updated : r)));
  }

  private toRequest(c: Course): CourseRequest {
    return {
      pkid: c.pkid,
      title: c.title,
      officialTitle: c.officialTitle ?? null,
      courseId: c.courseId,
      prodCourseId: c.prodCourseId,
      friendlyUrl: c.friendlyUrl,
      displayOrder: c.displayOrder,
      partnerPkid: c.partnerPkid,
      courseGroupPkid: c.courseGroupPkid ?? null,
      publishStatusPkid: c.publishStatusPkid,
      scheduleOn: c.scheduleOn,
      scheduleOff: c.scheduleOff,
      hour: c.hour,
      listPrice: c.listPrice,
      learningCredit: c.learningCredit,
      material: c.material ?? null,
      objective: c.objective ?? null,
      target: c.target ?? null,
      prerequisites: c.prerequisites ?? null,
      outline: c.outline ?? null,
      towardCertOrExam: c.towardCertOrExam ?? null,
      note: c.note ?? null,
      otherInfo: c.otherInfo ?? null,
      canRepeat: c.canRepeat,
      certificationPkids: c.certificationPkids ?? [],
      jobCategoryPkids: c.jobCategoryPkids ?? [],
    };
  }

  // ==================================================================
  //  Filters / navigation
  // ==================================================================

  applyFilters(): void {
    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.toQuery()));
    this.pageState = { ...this.pageState, first: 0 };
    sessionStorage.setItem(PAGE_KEY, JSON.stringify(this.pageState));
    this.drawerVisible.set(false);
    this.load();
  }

  resetFilters(): void {
    this.filters = emptyFilters();
    sessionStorage.removeItem(FILTERS_KEY);
    this.applyFilters();
  }

  onSort(event: { field?: string | null; order?: number }): void {
    this.sortState = { sortField: event.field ?? null, sortOrder: event.order ?? 1 };
    sessionStorage.setItem(SORT_KEY, JSON.stringify(this.sortState));
  }

  onPage(event: TableLazyLoadEvent): void {
    this.pageState = { first: event.first ?? 0, rows: event.rows ?? this.pageState.rows };
    sessionStorage.setItem(PAGE_KEY, JSON.stringify(this.pageState));
  }

  view(course: Course): void {
    this.router.navigate(['/courses', course.pkid]);
  }

  edit(course: Course): void {
    this.router.navigate(['/courses', course.pkid, 'edit']);
  }

  add(): void {
    this.router.navigate(['/courses/new']);
  }

  confirmDelete(course: Course): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${course.pkid}</b>「${course.courseId} ${course.title}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.remove(course),
    });
  }

  private remove(course: Course): void {
    this.service.delete(course.pkid).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已刪除', detail: `課程「${course.title}」已刪除。` });
        this.load();
      },
      error: () =>
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除課程。' }),
    });
  }

  private restoreState(): void {
    const filters = sessionStorage.getItem(FILTERS_KEY);
    if (filters) {
      try {
        const q = JSON.parse(filters) as CourseQuery;
        this.filters = {
          keyword: q.keyword ?? null,
          partnerPkid: q.partnerPkid ?? null,
          courseGroupPkid: q.courseGroupPkid ?? null,
          publishStatusPkid: q.publishStatusPkid ?? null,
          scheduleOnFrom: q.scheduleOnFrom ? new Date(q.scheduleOnFrom) : null,
          scheduleOnTo: q.scheduleOnTo ? new Date(q.scheduleOnTo) : null,
          scheduleOffFrom: q.scheduleOffFrom ? new Date(q.scheduleOffFrom) : null,
          scheduleOffTo: q.scheduleOffTo ? new Date(q.scheduleOffTo) : null,
          canRepeat: q.canRepeat ?? null,
        };
      } catch {
        /* ignore corrupt state */
      }
    }
    const sort = sessionStorage.getItem(SORT_KEY);
    if (sort) {
      try {
        this.sortState = { ...this.sortState, ...JSON.parse(sort) };
      } catch {
        /* ignore */
      }
    }
    const page = sessionStorage.getItem(PAGE_KEY);
    if (page) {
      try {
        this.pageState = { ...this.pageState, ...JSON.parse(page) };
      } catch {
        /* ignore */
      }
    }
  }
}
