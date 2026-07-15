import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';

import { PartnerService } from '@core/services/partner.service';
import { Partner, PartnerRequest } from '@core/models/partner.model';

@Component({
  selector: 'app-partner-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
  ],
  templateUrl: './partner-form.html',
  styleUrl: './partner-form.scss',
})
export class PartnerForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messages = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  // pkid is IDENTITY (auto-generated) — not a form field; carried internally for update.
  readonly pkid = signal(0);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    appKey: ['', [Validators.required, Validators.maxLength(10)]],
    nameOnPartnerMenu: ['', [Validators.required, Validators.maxLength(200)]],
    nameOnCourseDetailPage: ['', [Validators.required, Validators.maxLength(50)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    imageFilename: this.fb.control<string | null>(null, [Validators.maxLength(50)]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(idParam != null);

    if (idParam != null) {
      this.service.getById(Number(idParam)).subscribe({
        next: (partner) => {
          this.pkid.set(partner.pkid);
          this.form.patchValue({
            name: partner.name,
            appKey: partner.appKey,
            nameOnPartnerMenu: partner.nameOnPartnerMenu,
            nameOnCourseDetailPage: partner.nameOnCourseDetailPage,
            displayOrder: partner.displayOrder,
            imageFilename: partner.imageFilename ?? null,
          });
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得合作廠商資料。' });
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
    const request: PartnerRequest = {
      pkid: this.pkid(),
      name: raw.name,
      appKey: raw.appKey,
      nameOnPartnerMenu: raw.nameOnPartnerMenu,
      nameOnCourseDetailPage: raw.nameOnCourseDetailPage,
      displayOrder: raw.displayOrder,
      imageFilename: raw.imageFilename,
    };

    this.saving.set(true);
    const op$: Observable<Partner | void> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);

    op$.subscribe({
      next: (result) => {
        this.saving.set(false);
        this.messages.add({
          severity: 'success',
          summary: this.isEdit() ? '已更新' : '已新增',
          detail: `合作廠商「${request.name}」已儲存。`,
        });
        // On create the new pkid comes back in the response; on edit reuse the carried pkid.
        const targetId = this.isEdit() ? request.pkid : (result as Partner).pkid;
        this.router.navigate(['/partners', targetId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        void err;
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/partners']);
  }

  invalid(
    control: 'name' | 'appKey' | 'nameOnPartnerMenu' | 'nameOnCourseDetailPage' | 'displayOrder',
  ): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }
}
