import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

import { CourseQrCode } from './course-qr-code';

describe('CourseQrCode', () => {
  let fixture: ComponentFixture<CourseQrCode>;
  let component: CourseQrCode;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseQrCode],
      providers: [provideNoopAnimations(), providePrimeNG({ theme: { preset: Aura } })],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseQrCode);
    component = fixture.componentInstance;
    component.pkid = 42;
    component.courseId = 'AZ-900';
  });

  it('encodes the expected public course URL from pkid + courseId', () => {
    expect(component.url).toBe('https://www.uuu.com.tw/Course/Show/42/AZ-900');
  });

  it('shows the CourseId as the title', () => {
    fixture.detectChanges();
    const title: HTMLElement = fixture.nativeElement.querySelector('.qr-title');
    expect(title.textContent?.trim()).toBe('AZ-900');
  });

  it('generates a PNG image data URL for the encoded URL and renders it', async () => {
    const dataUrl = await component.generate();

    expect(dataUrl).toMatch(/^data:image\/png/);
    expect(component.dataUrl()).toBe(dataUrl);

    fixture.detectChanges();
    const img: HTMLImageElement = fixture.nativeElement.querySelector('img.qr-img');
    expect(img.getAttribute('src')).toBe(dataUrl);
  });

  it('download produces an image file named after the CourseId', async () => {
    await component.generate();

    const anchor = document.createElement('a');
    const clickSpy = spyOn(anchor, 'click');
    spyOn(document, 'createElement').and.returnValue(anchor);

    component.download();

    expect(anchor.href).toMatch(/^data:image\/png/); // a real image, not a page link
    expect(anchor.download).toBe('AZ-900.png');
    expect(clickSpy).toHaveBeenCalled();
  });

  it('download does nothing before an image has been generated', () => {
    const clickSpy = jasmine.createSpy('click');
    spyOn(document, 'createElement').and.returnValue({ click: clickSpy } as unknown as HTMLElement);

    component.download(); // dataUrl signal is still null

    expect(clickSpy).not.toHaveBeenCalled();
  });
});
