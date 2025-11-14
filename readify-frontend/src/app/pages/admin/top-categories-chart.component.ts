import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminDashboardService } from '../../services/admin-dashboard.service';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'top-categories-chart',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <mat-card class="chart-card">
      <mat-card-title>Top Categories</mat-card-title>
      <mat-card-content>
        <div class="chart-wrap"><canvas id="topCategoriesCanvasChild"></canvas></div>
      </mat-card-content>
    </mat-card>
  `
})
export class TopCategoriesChartComponent implements OnChanges {
  @Input() from?: string | null;
  @Input() to?: string | null;
  @Input() top: number = 8;

  private chart: any | null = null;
  constructor(private svc: AdminDashboardService) {}

  ngOnChanges(changes: SimpleChanges): void { this.load(); }

  async load() {
    try {
      const r: any = await this.svc.getTopCategories(this.top, this.from || undefined, this.to || undefined).toPromise();
      const labels = r?.labels || [];
      const values = r?.values || [];
      await this.render(labels, values);
    } catch (ex) { console.warn('Top categories load failed', ex); }
  }

  private async render(labels: string[], values: any[]) {
    const mod = await import('chart.js/auto');
    const ChartCtor = (mod as any).default ?? mod;
    const el = document.getElementById('topCategoriesCanvasChild') as HTMLCanvasElement | null;
    if (!el) return;
    const ctx = el.getContext('2d');
    if (!ctx) return;
    try { this.chart?.destroy(); } catch {}
    this.chart = new ChartCtor(ctx, { type: 'bar', data: { labels, datasets: [{ label: 'Revenue', data: values, backgroundColor: '#4caf50' }] }, options: { responsive: true, maintainAspectRatio: false } });
  }
}
