import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../environments/environment';

// Standalone pipe to centralize date formatting with configurable default timezone
@Pipe({ name: 'localDate', standalone: true })
export class LocalDatePipe implements PipeTransform {
  transform(value: any, format: string = 'medium', timeZone?: string): string | null {
    if (value === null || value === undefined || value === '') return null;

    const date = (value instanceof Date) ? value : new Date(value);
    if (isNaN(date.getTime())) return null;

    // Resolve timezone: parameter > browser setting > environment > fallback to undefined (use runtime locale)
    const browserTz = (() => {
      try { return Intl.DateTimeFormat().resolvedOptions().timeZone; } catch { return undefined; }
    })();

    const tz = timeZone ?? browserTz ?? (environment?.defaultTimezone) ?? undefined;

    // Map a few common Angular-like formats to Intl options
    const opts: Intl.DateTimeFormatOptions = {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: 'numeric', minute: 'numeric'
    };

    switch (format) {
      case 'short':
        Object.assign(opts, { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric' });
        break;
      case 'medium':
        Object.assign(opts, { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: 'numeric' });
        break;
      case 'long':
        Object.assign(opts, { year: 'numeric', month: 'long', day: 'numeric', hour: 'numeric', minute: 'numeric', second: 'numeric', timeZoneName: 'short' });
        break;
      case 'full':
        Object.assign(opts, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric', hour: 'numeric', minute: 'numeric', second: 'numeric', timeZoneName: 'short' });
        break;
      default:
        // allow custom patterns by falling back to medium-like behavior
        break;
    }

    try {
      // If tz is undefined Intl will use the runtime environment's timezone
      return new Intl.DateTimeFormat(undefined, { ...opts, timeZone: tz }).format(date);
    } catch (e) {
      return date.toISOString();
    }
  }
}
