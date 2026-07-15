import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of, Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';

import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course, CourseRequest } from '@core/models/course.model';
import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';
import { CertificationLookup } from '@core/models/certification-lookup.model';
import { JobCategoryLookup } from '@core/models/job-category-lookup.model';
import { addYears, fromIso, toIso } from './course-date.util';

type RequiredControl =
  | 'title'
  | 'courseId'
  | 'prodCourseId'
  | 'friendlyUrl'
  | 'displayOrder'
  | 'partnerPkid'
  | 'publishStatusPkid'
  | 'scheduleOn'
  | 'scheduleOff'
  | 'hour'
  | 'listPrice'
  | 'learningCredit';

@Component({
  selector: 'app-course-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    InputNumberModule,
    SelectModule,
    MultiSelectModule,
    DatePickerModule,
    CheckboxModule,
  ],
  templateUrl: './course-form.html',
  styleUrl: './course-form.scss',
})
export class CourseForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  // pkid is IDENTITY — carried internally, not a form field.
  readonly pkid = signal(0);

  readonly partners = signal<PartnerLookup[]>([]);
  readonly courseGroups = signal<CourseGroupLookup[]>([]);
  readonly publishStatuses = signal<PublishStatusLookup[]>([]);
  readonly certifications = signal<CertificationLookup[]>([]);
  readonly jobCategories = signal<JobCategoryLookup[]>([]);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    officialTitle: this.fb.control<string | null>(null, [Validators.maxLength(300)]),
    courseId: ['', [Validators.required, Validators.maxLength(50)]],
    prodCourseId: ['', [Validators.required, Validators.maxLength(50)]],
    friendlyUrl: ['', [Validators.required, Validators.maxLength(100)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    partnerPkid: this.fb.control<number | null>(null, [Validators.required]),
    courseGroupPkid: this.fb.control<number | null>(null),
    publishStatusPkid: this.fb.control<number | null>(null, [Validators.required]),
    scheduleOn: this.fb.control<Date | null>(null, [Validators.required]),
    scheduleOff: this.fb.control<Date | null>(null, [Validators.required]),
    hour: [0, [Validators.required, Validators.min(0)]],
    listPrice: [0, [Validators.required, Validators.min(0)]],
    learningCredit: [0, [Validators.required, Validators.min(0)]],
    material: this.fb.control<string | null>(null, [Validators.maxLength(500)]),
    objective: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    target: this.fb.control<string | null>(null, [Validators.maxLength(500)]),
    prerequisites: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    outline: this.fb.control<string | null>(null),
    towardCertOrExam: this.fb.control<string | null>(null),
    note: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    otherInfo: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    canRepeat: [false],
    certificationPkids: this.fb.nonNullable.control<number[]>([]),
    jobCategoryPkids: this.fb.nonNullable.control<number[]>([]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(idParam != null);

    // When ScheduleOn changes, default ScheduleOff to +10 years (silent — no feedback loop).
    // In edit mode the loaded ScheduleOff still wins because it is patched *after* ScheduleOn (below).
    this.form.controls.scheduleOn.valueChanges.subscribe((on) => {
      if (on) this.form.controls.scheduleOff.setValue(addYears(on, 10), { emitEvent: false });
    });

    forkJoin({
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
      certifications: this.lookups.getCertifications(),
      jobCategories: this.lookups.getJobCategories(),
      course: idParam ? this.service.getById(Number(idParam)) : of(null),
    }).subscribe({
      next: ({ partners, courseGroups, publishStatuses, certifications, jobCategories, course }) => {
        this.partners.set(partners);
        this.courseGroups.set(courseGroups);
        this.publishStatuses.set(publishStatuses);
        this.certifications.set(certifications);
        this.jobCategories.set(jobCategories);
        if (course) this.patchFrom(course);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得表單資料。' });
      },
    });
  }

  private patchFrom(course: Course): void {
    this.pkid.set(course.pkid);
    // scheduleOn is patched before scheduleOff so its valueChanges default is overwritten by the real value.
    this.form.patchValue({
      title: course.title,
      officialTitle: course.officialTitle ?? null,
      courseId: course.courseId,
      prodCourseId: course.prodCourseId,
      friendlyUrl: course.friendlyUrl,
      displayOrder: course.displayOrder,
      partnerPkid: course.partnerPkid,
      courseGroupPkid: course.courseGroupPkid ?? null,
      publishStatusPkid: course.publishStatusPkid,
      scheduleOn: fromIso(course.scheduleOn),
      scheduleOff: fromIso(course.scheduleOff),
      hour: course.hour,
      listPrice: course.listPrice,
      learningCredit: course.learningCredit,
      material: course.material ?? null,
      objective: course.objective ?? null,
      target: course.target ?? null,
      prerequisites: course.prerequisites ?? null,
      outline: course.outline ?? null,
      towardCertOrExam: course.towardCertOrExam ?? null,
      note: course.note ?? null,
      otherInfo: course.otherInfo ?? null,
      canRepeat: course.canRepeat,
      certificationPkids: course.certificationPkids,
      jobCategoryPkids: course.jobCategoryPkids,
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: CourseRequest = {
      pkid: this.pkid(),
      title: raw.title,
      officialTitle: raw.officialTitle,
      courseId: raw.courseId,
      prodCourseId: raw.prodCourseId,
      friendlyUrl: raw.friendlyUrl,
      displayOrder: raw.displayOrder,
      partnerPkid: raw.partnerPkid!,
      courseGroupPkid: raw.courseGroupPkid ?? null,
      publishStatusPkid: raw.publishStatusPkid!,
      scheduleOn: toIso(raw.scheduleOn)!,
      scheduleOff: toIso(raw.scheduleOff)!,
      hour: raw.hour,
      listPrice: raw.listPrice,
      learningCredit: raw.learningCredit,
      material: raw.material,
      objective: raw.objective,
      target: raw.target,
      prerequisites: raw.prerequisites,
      outline: raw.outline,
      towardCertOrExam: raw.towardCertOrExam,
      note: raw.note,
      otherInfo: raw.otherInfo,
      canRepeat: raw.canRepeat,
      certificationPkids: raw.certificationPkids,
      jobCategoryPkids: raw.jobCategoryPkids,
    };

    this.saving.set(true);
    const op$: Observable<Course | void> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);

    op$.subscribe({
      next: (result) => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit() ? '已更新' : '已新增',
          detail: `課程「${request.title}」已儲存。`,
        });
        const targetId = this.isEdit() ? request.pkid : (result as Course).pkid;
        this.router.navigate(['/courses', targetId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        void err;
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/courses']);
  }

  invalid(control: RequiredControl): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
