import { AfterViewInit, Directive, ElementRef, inject } from '@angular/core';

/**
 * Focuses the host (or its first inner focusable) when it enters the DOM — used to focus an inline
 * cell editor the moment double-click opens it, so the user can type without a second click.
 */
@Directive({
  selector: '[appAutofocus]',
})
export class Autofocus implements AfterViewInit {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  ngAfterViewInit(): void {
    const el = this.host.nativeElement;
    const target = el.matches('input, textarea, select')
      ? el
      : el.querySelector('input, textarea, select, [tabindex]');
    (target as HTMLElement | null)?.focus();
  }
}
