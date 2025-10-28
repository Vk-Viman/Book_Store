import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-container" [attr.aria-label]="'Loading ' + type">
      <div *ngIf="type === 'card'" class="skeleton-card">
        <div class="skeleton skeleton-image"></div>
        <div class="skeleton skeleton-title"></div>
        <div class="skeleton skeleton-text"></div>
        <div class="skeleton skeleton-text" style="width: 60%;"></div>
      </div>
      <div *ngIf="type === 'list'" class="skeleton-list">
        <div *ngFor="let item of [1,2,3,4,5,6]" class="skeleton-list-item">
          <div class="skeleton skeleton-text"></div>
        </div>
      </div>
      <div *ngIf="type === 'text'" class="skeleton-text-container">
        <div *ngFor="let line of lines" class="skeleton skeleton-text" [style.width]="line === lines.length ? '60%' : '100%'"></div>
      </div>
    </div>
  `,
  styles: [`
    .skeleton-container {
      padding: 16px;
    }
    .skeleton-card {
      background: white;
      border-radius: 4px;
      padding: 16px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .skeleton-list-item {
      margin-bottom: 12px;
    }
    .skeleton-text-container .skeleton {
      margin-bottom: 8px;
    }
  `]
})
export class LoadingSkeletonComponent {
  @Input() type: 'card' | 'list' | 'text' = 'card';
  @Input() count: number = 1;
  
  get lines(): number[] {
    return Array(this.count).fill(0).map((_, i) => i);
  }
}
