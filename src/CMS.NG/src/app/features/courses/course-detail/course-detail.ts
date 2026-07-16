import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';

import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';
import { CertificationLookup } from '@core/models/certification-lookup.model';
import { JobCategoryLookup } from '@core/models/job-category-lookup.model';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';
import { CourseQrCode } from './course-qr-code/course-qr-code';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [RouterLink, ButtonModule, TagModule, CourseQrCode, RowAuditBadge],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  readonly course = signal<Course | null>(null);
  readonly loading = signal(true);
  private readonly certifications = signal<CertificationLookup[]>([]);
  private readonly jobCategories = signal<JobCategoryLookup[]>([]);

  readonly certificationLabels = computed(() => {
    const map = new Map(this.certifications().map((c) => [c.pkid, c.label]));
    return (this.course()?.certificationPkids ?? []).map((id) => map.get(id) ?? `#${id}`);
  });

  readonly jobCategoryLabels = computed(() => {
    const map = new Map(this.jobCategories().map((j) => [j.pkid, j.label]));
    return (this.course()?.jobCategoryPkids ?? []).map((id) => map.get(id) ?? `#${id}`);
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    forkJoin({
      course: this.service.getById(id),
      certifications: this.lookups.getCertifications(),
      jobCategories: this.lookups.getJobCategories(),
    }).subscribe({
      next: ({ course, certifications, jobCategories }) => {
        this.course.set(course);
        this.certifications.set(certifications);
        this.jobCategories.set(jobCategories);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程資料。' });
      },
    });
  }

  edit(): void {
    const id = this.course()?.pkid;
    if (id != null) this.router.navigate(['/courses', id, 'edit']);
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
