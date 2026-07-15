import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';

import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus, PublishStatusRequest } from '@core/models/publish-status.model';

@Component({
  selector: 'app-publish-status-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    CheckboxModule,
  ],
  templateUrl: './publish-status-form.html',
  styleUrl: './publish-status-form.scss',
})
export class PublishStatusForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PublishStatusService);
  private readonly messages = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  private pkid: number | null = null;

  readonly form = this.fb.nonNullable.group({
    pkid: [0, [Validators.required, Validators.min(0), Validators.max(255)]],
    description: ['', [Validators.required, Validators.maxLength(50)]],
    isDraft: [false],
    isPublished: [false],
    isDiscontinued: [false],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.pkid = idParam != null ? Number(idParam) : null;
    this.isEdit.set(this.pkid != null);

    if (this.pkid != null) {
      this.service.getById(this.pkid).subscribe({
        next: (status) => {
          this.form.patchValue({
            pkid: status.pkid,
            description: status.description,
            isDraft: status.isDraft,
            isPublished: status.isPublished,
            isDiscontinued: status.isDiscontinued,
          });
          this.form.controls.pkid.disable(); // pkid is the PK — immutable on edit.
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得發布狀態資料。' });
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
    const request: PublishStatusRequest = {
      pkid: raw.pkid,
      description: raw.description,
      isDraft: raw.isDraft,
      isPublished: raw.isPublished,
      isDiscontinued: raw.isDiscontinued,
    };

    this.saving.set(true);
    const op$: Observable<PublishStatus | void> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit() ? '已更新' : '已新增',
          detail: `發布狀態「${request.description}」已儲存。`,
        });
        this.router.navigate(['/publish-statuses', request.pkid]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? `主代碼「${request.pkid}」已存在。` : '儲存失敗，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/publish-statuses']);
  }

  invalid(control: 'pkid' | 'description'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
