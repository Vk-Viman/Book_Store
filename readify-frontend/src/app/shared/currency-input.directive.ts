import { Directive, HostListener, ElementRef } from '@angular/core';

@Directive({ selector: '[currencyInput]' })
export class CurrencyInputDirective {
  private el: HTMLInputElement;
  constructor(private elementRef: ElementRef) { this.el = this.elementRef.nativeElement; }

  @HostListener('input', ['$event']) onInput(event: any) {
    const raw = this.el.value;
    // allow digits, optional decimal point and up to 2 decimals
    const cleaned = raw.replace(/[^0-9.]/g, '');
    const parts = cleaned.split('.');
    if (parts.length > 2) {
      this.el.value = parts[0] + '.' + parts[1].slice(0,2);
      return;
    }
    if (parts[1]) parts[1] = parts[1].slice(0,2);
    this.el.value = parts.join('.');
  }
}
