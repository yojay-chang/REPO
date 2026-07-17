import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

import { QrCode } from './qr-code';
import { QrCodeService } from '@core/services/qr-code.service';

const URL = 'https://www.uuu.com.tw/Course/Show/1/AZ-900';

describe('QrCode', () => {
  function setup() {
    TestBed.configureTestingModule({
      imports: [QrCode],
      providers: [provideNoopAnimations()],
    });
    const fixture = TestBed.createComponent(QrCode);
    const service = TestBed.inject(QrCodeService);
    return { fixture, service };
  }

  it('encodes the provided URL and produces a PNG image data URL', async () => {
    const { fixture, service } = setup();
    const encodeSpy = spyOn(service, 'toDataUrl').and.callThrough();

    fixture.componentRef.setInput('value', URL);
    fixture.componentRef.setInput('title', 'AZ-900');
    fixture.detectChanges();
    await fixture.whenStable();

    // Encodes exactly the URL it was given…
    expect(encodeSpy).toHaveBeenCalledWith(URL);
    // …and the result is a real PNG image.
    const dataUrl = fixture.componentInstance.dataUrl();
    expect(dataUrl).toContain('data:image/png');
  });

  it('shows the title above the QR image', () => {
    const { fixture } = setup();
    fixture.componentRef.setInput('value', URL);
    fixture.componentRef.setInput('title', 'AZ-900');
    fixture.detectChanges();

    const title = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qr-title"]');
    expect(title?.textContent?.trim()).toBe('AZ-900');
  });

  it('renders the encoded image in an <img> once generated', async () => {
    const { fixture } = setup();
    fixture.componentRef.setInput('value', URL);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const img = (fixture.nativeElement as HTMLElement).querySelector('img.qr-image') as HTMLImageElement | null;
    expect(img).not.toBeNull();
    expect(img!.src).toContain('data:image/png');
  });

  it('download action produces a PNG image download named after the file name', () => {
    const { fixture } = setup();
    fixture.componentRef.setInput('value', URL);
    fixture.componentRef.setInput('fileName', 'AZ-900');
    // Seed a deterministic image so the assertion does not depend on async encoding.
    fixture.componentInstance.dataUrl.set('data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA=');

    const createReal = document.createElement.bind(document);
    const anchor = createReal('a') as HTMLAnchorElement;
    const clickSpy = spyOn(anchor, 'click');
    spyOn(document, 'createElement').and.callFake((tag: string) =>
      tag === 'a' ? anchor : createReal(tag),
    );

    fixture.componentInstance.download();

    expect(clickSpy).toHaveBeenCalled();
    expect(anchor.download).toBe('AZ-900.png');
    expect(anchor.href.startsWith('data:image/png')).toBeTrue();
  });

  it('does not download when no image has been generated', () => {
    const { fixture } = setup();
    fixture.componentRef.setInput('value', '');
    fixture.componentInstance.dataUrl.set(null);

    const createSpy = spyOn(document, 'createElement').and.callThrough();
    fixture.componentInstance.download();

    expect(createSpy).not.toHaveBeenCalled();
  });
});
