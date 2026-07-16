import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';

import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup, CourseGroupRequest } from '@core/models/course-group.model';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-course-group-form',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, RowAuditBadge],
  templateUrl: './course-group-form.html',
  styleUrl: './course-group-form.scss',
})
export class CourseGroupForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messages = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  // pkid is IDENTITY (auto-generated) — not a form field; carried internally for update.
  readonly pkid = signal(0);

  readonly form = this.fb.nonNullable.group({
    description: ['', [Validators.required, Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(idParam != null);

    if (idParam != null) {
      this.service.getById(Number(idParam)).subscribe({
        next: (group) => {
          this.pkid.set(group.pkid);
          this.form.patchValue({ description: group.description });
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程群組資料。' });
        },
      });
    } else {
      this.loading.set(false);
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: CourseGroupRequest = {
      pkid: this.pkid(),
      description: raw.description,
    };

    this.saving.set(true);
    const op$: Observable<CourseGroup | void> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);

    op$.subscribe({
      next: (result) => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit() ? '已更新' : '已新增',
          detail: `課程群組「${request.description}」已儲存。`,
        });
        // On create the new pkid comes back in the response; on edit reuse the carried pkid.
        const targetId = this.isEdit() ? request.pkid : (result as CourseGroup).pkid;
        this.router.navigate(['/course-groups', targetId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        void err;
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/course-groups']);
  }

  invalid(control: 'description'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
