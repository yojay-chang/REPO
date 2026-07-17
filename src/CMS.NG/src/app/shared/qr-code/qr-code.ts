import { Component, effect, inject, input, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';

import { QrCodeService } from '@core/services/qr-code.service';

/**
 * Reusable QR-code tile: renders a heading, the encoded QR image, and a download button.
 * The parent passes the fully-built {@link value} URL — this component only encodes/displays it.
 */
@Component({
  selector: 'app-qr-code',
  imports: [ButtonModule],
  templateUrl: './qr-code.html',
  styleUrl: './qr-code.scss',
})
export class QrCode {
  private readonly qrCode = inject(QrCodeService);

  /** Text/URL to encode. */
  readonly value = input.required<string>();
  /** Heading shown above the QR image. */
  readonly title = input<string>('');
  /** Base file name (no extension) for the downloaded PNG; falls back to the title. */
  readonly fileName = input<string>('');

  /** The generated PNG `data:` URL, or null while empty/failed. */
  readonly dataUrl = signal<string | null>(null);

  constructor() {
    // Re-encode whenever the value changes.
    effect(() => {
      const value = this.value();
      if (!value) {
        this.dataUrl.set(null);
        return;
      }
      this.qrCode
        .toDataUrl(value)
        .then((url) => this.dataUrl.set(url))
        .catch(() => this.dataUrl.set(null));
    });
  }

  /** Trigger a browser download of the current QR image as a PNG. */
  download(): void {
    const url = this.dataUrl();
    if (!url) return;

    const base = (this.fileName() || this.title() || 'qr-code').trim() || 'qr-code';
    const link = document.createElement('a');
    link.href = url;
    link.download = `${base}.png`;
    link.click();
  }
}
