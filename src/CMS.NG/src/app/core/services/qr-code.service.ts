import { Injectable } from '@angular/core';
import * as QRCode from 'qrcode';

/** Thin wrapper around the `qrcode` library so components stay easy to unit-test. */
@Injectable({ providedIn: 'root' })
export class QrCodeService {
  /** Encode `value` as a PNG `data:` URL suitable for an `<img src>` or a download link. */
  toDataUrl(value: string): Promise<string> {
    return QRCode.toDataURL(value, { width: 240, margin: 1, errorCorrectionLevel: 'M' });
  }
}
