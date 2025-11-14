import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'highlight', standalone: true })
export class HighlightPipe implements PipeTransform {
  transform(value: string | null | undefined, query: string | null | undefined): string {
    if (!value) return '';
    if (!query) return value;
    try {
      const terms = (query || '').split(/\s+/).filter(t => t.trim().length > 0);
      if (!terms.length) return value;
      // escape regex
      const esc = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const regex = new RegExp('(' + terms.map(esc).join('|') + ')', 'ig');
      return value.replace(regex, '<mark>$1</mark>');
    } catch {
      return value;
    }
  }
}
