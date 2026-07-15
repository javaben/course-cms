import { Component, Input, OnChanges, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import * as QRCode from 'qrcode';

/**
 * Renders a downloadable QR code for a course's public page.
 * The encoded URL is <c>https://www.uuu.com.tw/Course/Show/{pkid}/{courseId}</c> and the
 * CourseId is shown as the caption/title. The image is generated as a PNG data URL, so the
 * download needs no server round-trip.
 */
@Component({
  selector: 'app-course-qr-code',
  standalone: true,
  imports: [ButtonModule],
  templateUrl: './course-qr-code.html',
  styleUrl: './course-qr-code.scss',
})
export class CourseQrCode implements OnChanges {
  @Input({ required: true }) pkid!: number;
  @Input({ required: true }) courseId!: string;

  /** Generated PNG data URL (null until the first generation resolves). */
  readonly dataUrl = signal<string | null>(null);

  /** The public course URL the QR encodes. */
  get url(): string {
    return `https://www.uuu.com.tw/Course/Show/${this.pkid}/${this.courseId}`;
  }

  ngOnChanges(): void {
    void this.generate();
  }

  /** Generate the QR image as a PNG data URL; stores it in the signal and returns it. */
  async generate(): Promise<string> {
    const dataUrl = await QRCode.toDataURL(this.url, { width: 220, margin: 1 });
    this.dataUrl.set(dataUrl);
    return dataUrl;
  }

  /** Trigger a browser download of the generated image as {courseId}.png. */
  download(): void {
    const dataUrl = this.dataUrl();
    if (!dataUrl) return;
    const anchor = document.createElement('a');
    anchor.href = dataUrl;
    anchor.download = `${this.courseId}.png`;
    anchor.click();
  }
}
