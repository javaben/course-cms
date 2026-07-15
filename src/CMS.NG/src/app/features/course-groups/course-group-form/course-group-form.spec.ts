import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CourseGroupForm } from './course-group-form';

const groupUrl = (id: number) => `${environment.apiBaseUrl}/api/course-groups/${id}`;
const base = `${environment.apiBaseUrl}/api/course-groups`;

function configure(id: string | null): {
  fixture: ComponentFixture<CourseGroupForm>;
  component: CourseGroupForm;
  http: HttpTestingController;
} {
  TestBed.configureTestingModule({
    imports: [CourseGroupForm],
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
  const fixture = TestBed.createComponent(CourseGroupForm);
  return {
    fixture,
    component: fixture.componentInstance,
    http: TestBed.inject(HttpTestingController),
  };
}

describe('CourseGroupForm', () => {
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('starts in add mode with an invalid empty form', () => {
    const { fixture, component } = configure(null);
    fixture.detectChanges();

    expect(component.isEdit()).toBeFalse();
    expect(component.pkid()).toBe(0);
    expect(component.form.valid).toBeFalse(); // description empty
  });

  it('loads the group and carries its pkid in edit mode', () => {
    const { fixture, component, http } = configure('1');
    fixture.detectChanges();

    http.expectOne(groupUrl(1)).flush({ pkid: 1, description: '資訊技術' });

    expect(component.isEdit()).toBeTrue();
    expect(component.pkid()).toBe(1);
    expect(component.form.controls.description.value).toBe('資訊技術');
  });

  it('saves via POST when adding', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();

    component.form.patchValue({ description: '資料科學' });
    component.save();

    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.description).toBe('資料科學');
    expect(req.request.body.pkid).toBe(0); // IDENTITY — server assigns
    req.flush({ pkid: 9, description: '資料科學' });
  });
});
