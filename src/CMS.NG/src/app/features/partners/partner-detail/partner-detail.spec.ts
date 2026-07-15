import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { PartnerDetail } from './partner-detail';

const PARTNER_URL = `${environment.apiBaseUrl}/api/partners/1`;

describe('PartnerDetail', () => {
  let fixture: ComponentFixture<PartnerDetail>;
  let component: PartnerDetail;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PartnerDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PartnerDetail);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the partner by pkid', () => {
    fixture.detectChanges();

    http.expectOne(PARTNER_URL).flush({
      pkid: 1,
      name: '微軟',
      appKey: 'MS',
      nameOnPartnerMenu: 'Microsoft 微軟',
      nameOnCourseDetailPage: '微軟',
      displayOrder: 2,
      imageFilename: 'ms.png',
    });

    expect(component.partner()?.name).toBe('微軟');
    expect(component.partner()?.appKey).toBe('MS');
  });
});
