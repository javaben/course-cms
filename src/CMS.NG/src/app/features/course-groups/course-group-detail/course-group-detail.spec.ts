import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { environment } from '@env/environment';

import { CourseGroupDetail } from './course-group-detail';

const GROUP_URL = `${environment.apiBaseUrl}/api/course-groups/1`;

describe('CourseGroupDetail', () => {
  let fixture: ComponentFixture<CourseGroupDetail>;
  let component: CourseGroupDetail;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseGroupDetail],
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

    fixture = TestBed.createComponent(CourseGroupDetail);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the group by pkid', () => {
    fixture.detectChanges();

    http.expectOne(GROUP_URL).flush({ pkid: 1, description: '資訊技術' });

    expect(component.group()?.description).toBe('資訊技術');
  });
});
