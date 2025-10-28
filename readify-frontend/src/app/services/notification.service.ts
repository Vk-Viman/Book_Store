import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

export type Notice = { type: 'success' | 'error' | 'info'; text: string; timeout?: number };

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private _messages = new BehaviorSubject<Notice | null>(null);
  messages$ = this._messages.asObservable();

  constructor(private snack: MatSnackBar) {}

  private open(text: string, panelClass: string[], timeout = 3000) {
    this.snack.open(text, 'Close', { duration: timeout, panelClass });
  }

  success(text: string, timeout = 3000) {
    this._messages.next({ type: 'success', text, timeout });
    this.open(text, ['snack-success'], timeout);
  }
  error(text: string, timeout = 5000) {
    this._messages.next({ type: 'error', text, timeout });
    this.open(text, ['snack-error'], timeout);
  }
  info(text: string, timeout = 3000) {
    this._messages.next({ type: 'info', text, timeout });
    this.open(text, ['snack-info'], timeout);
  }
}
