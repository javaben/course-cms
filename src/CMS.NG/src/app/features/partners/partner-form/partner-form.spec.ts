import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { PartnerForm } from './partner-form';

const partnerUrl = (id: number) => `${environment.apiBaseUrl}/api/partners/${id}`;
const base = `${environment.apiBaseUrl}/api/partners`;

function configure(id: string | null): {
  fixture: ComponentFixture<PartnerForm>;
  component: PartnerForm;
  http: HttpTestingController;
} {
  TestBed.configureTestingModule({
    imports: [PartnerForm],
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
  const fixture = TestBed.createComponent(PartnerForm);
  return {
    fixture,
    component: fixture.componentInstance,
    http: TestBed.inject(HttpTestingController),
  };
}

describe('PartnerForm', () => {
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.resetTestingModule();
  });

  it('starts in add mode with an invalid empty form', () => {
    const { fixture, component } = configure(null);
    fixture.detectChanges();

    expect(component.isEdit()).toBeFalse();
    expect(component.pkid()).toBe(0);
    expect(component.form.valid).toBeFalse(); // required fields empty
  });

  it('loads the partner and carries its pkid in edit mode', () => {
    const { fixture, component, http } = configure('1');
    fixture.detectChanges();

    http.expectOne(partnerUrl(1)).flush({
      pkid: 1,
      name: '微軟',
      appKey: 'MS',
      nameOnPartnerMenu: 'Microsoft 微軟',
      nameOnCourseDetailPage: '微軟',
      displayOrder: 2,
      imageFilename: 'ms.png',
    });

    expect(component.isEdit()).toBeTrue();
    expect(component.pkid()).toBe(1);
    expect(component.form.controls.name.value).toBe('微軟');
  });

  it('saves via POST when adding', () => {
    const { fixture, component, http } = configure(null);
    fixture.detectChanges();

    component.form.patchValue({
      name: '亞馬遜',
      appKey: 'AWS',
      nameOnPartnerMenu: 'AWS 亞馬遜',
      nameOnCourseDetailPage: '亞馬遜',
      displayOrder: 3,
    });
    component.save();

    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('亞馬遜');
    expect(req.request.body.pkid).toBe(0); // IDENTITY — server assigns
    req.flush({ pkid: 9, name: '亞馬遜', appKey: 'AWS', nameOnPartnerMenu: 'AWS 亞馬遜', nameOnCourseDetailPage: '亞馬遜', displayOrder: 3, imageFilename: null });
  });
});
