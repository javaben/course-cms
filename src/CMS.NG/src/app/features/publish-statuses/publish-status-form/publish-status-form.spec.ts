import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { PublishStatusForm } from './publish-status-form';

const statusUrl = (id: number) => `${environment.apiBaseUrl}/api/publish-statuses/${id}`;
const base = `${environment.apiBaseUrl}/api/publish-statuses`;

function configure(id: string | null): {
  fixture: ComponentFixture<PublishStatusForm>;
  component: PublishStatusForm;
  http: HttpTestingController;
} {
  TestBed.configureTestingModule({
    imports: [PublishStatusForm],
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
  const fixture = TestBed.createComponent(PublishStatusForm);
  return {
    fixture,
    component: fixture.componentInstance,
    http: TestBed.inject(HttpTestingController),
  };
}

describe('PublishStatusForm', () => {
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('starts in add mode with an editable pkid and invalid form', () => {
    const { fixture, component } = configure(null);
    fixture.detectChanges();

    expect(component.isEdit()).toBeFalse();
    expect(component.form.controls.pkid.disabled).toBeFalse();
    expect(component.form.valid).toBeFalse(); // description empty
  });

  it('loads the status and disables pkid in edit mode', () => {
    const { fixture, component, http } = configure('2');
    fixture.detectChanges();

    http.expectOne(statusUrl(2)).flush({
      pkid: 2,
      description: '已發布',
      isDraft: false,
      isPublished: true,
      isDiscontinued: false,
    });

    expect(component.isEdit()).toBeTrue();
    expect(component.form.controls.pkid.disabled).toBeTrue();
    expect(component.form.controls.description.value).toBe('已發布');
    expect(component.form.getRawValue().isPublished).toBeTrue();
  });

  it('saves via POST when adding', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();

    component.form.patchValue({ pkid: 4, description: '審核中', isDraft: true });
    component.save();

    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.pkid).toBe(4);
    expect(req.request.body.description).toBe('審核中');
    req.flush({});
  });
});
