import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { PublishStatusDetail } from './publish-status-detail';

const STATUS_URL = `${environment.apiBaseUrl}/api/publish-statuses/2`;

describe('PublishStatusDetail', () => {
  let fixture: ComponentFixture<PublishStatusDetail>;
  let component: PublishStatusDetail;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PublishStatusDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        MessageService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '2' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublishStatusDetail);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the status by pkid', () => {
    fixture.detectChanges();

    http.expectOne(STATUS_URL).flush({
      pkid: 2,
      description: '已發布',
      isDraft: false,
      isPublished: true,
      isDiscontinued: false,
    });

    expect(component.status()?.description).toBe('已發布');
    expect(component.status()?.isPublished).toBeTrue();
  });
});
