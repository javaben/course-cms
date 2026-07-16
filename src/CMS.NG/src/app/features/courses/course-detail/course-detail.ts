import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, forkJoin, timeout, TimeoutError } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
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
  imports: [RouterLink, ButtonModule, TagModule, TooltipModule, CourseQrCode, RowAuditBadge],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  /** ~15s guard so a hung generation can't spin the button forever. */
  private static readonly PdfTimeoutMs = 15_000;

  readonly course = signal<Course | null>(null);
  readonly loading = signal(true);
  /** True while a PDF is being generated/downloaded — drives the button loading + disabled state. */
  readonly pdfLoading = signal(false);
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

  /**
   * Download the course flyer PDF. Guards against double-submit (the button is disabled while
   * loading), saves the blob via a temporary object URL, and on failure decodes the blob error
   * body (so it isn't shown as "[object Blob]") and shows a status-appropriate toast.
   */
  async downloadPdf(): Promise<void> {
    const c = this.course();
    if (c == null || this.pdfLoading()) return;

    this.pdfLoading.set(true);
    let objectUrl: string | null = null;
    try {
      const blob = await firstValueFrom(
        this.service.downloadPdf(c.pkid).pipe(timeout({ each: CourseDetail.PdfTimeoutMs })),
      );
      objectUrl = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = objectUrl;
      anchor.download = `course-${c.pkid}.pdf`;
      anchor.click();
      this.messages.add({ severity: 'success', summary: 'PDF 已下載', detail: 'Downloaded' });
    } catch (err) {
      const detail = await this.describePdfError(err);
      this.messages.add({ severity: 'error', summary: '下載失敗 Download failed', detail });
    } finally {
      if (objectUrl != null) URL.revokeObjectURL(objectUrl);
      this.pdfLoading.set(false);
    }
  }

  /** Turn a failed PDF request into a human message, decoding the blob error body when present. */
  private async describePdfError(err: unknown): Promise<string> {
    if (err instanceof TimeoutError) return '產生逾時，請稍後再試 Timed out';
    if (err instanceof HttpErrorResponse) {
      // The response is a blob, so err.error is a Blob — read it as text or it renders "[object Blob]".
      let serverMsg = '';
      if (err.error instanceof Blob) {
        try {
          serverMsg = (await err.error.text()).trim();
        } catch {
          serverMsg = '';
        }
      } else if (typeof err.error === 'string') {
        serverMsg = err.error;
      }
      switch (err.status) {
        case 401:
          return '請重新登入 Please sign in again';
        case 404:
          return '找不到此課程 Course not found';
        case 500:
          return serverMsg || '伺服器產生 PDF 時發生錯誤 Server error';
        default:
          return serverMsg || `發生錯誤（${err.status}）`;
      }
    }
    return '發生未知錯誤 Unknown error';
  }

  edit(): void {
    const id = this.course()?.pkid;
    if (id != null) this.router.navigate(['/courses', id, 'edit']);
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
