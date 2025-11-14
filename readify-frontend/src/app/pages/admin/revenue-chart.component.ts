import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminDashboardService } from '../../services/admin-dashboard.service';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'revenue-chart',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <mat-card class="chart-card">
      <mat-card-title>Revenue</mat-card-title>
      <mat-card-content>
        <div class="chart-wrap"><canvas id="revenueCanvasChild"></canvas></div>
      </mat-card-content>
    </mat-card>
  `
})
export class RevenueChartComponent implements OnChanges {
  @Input() from?: string | null;
  @Input() to?: string | null;
  @Input() categoryId?: number | null;
  @Input() period: number = 30;

  private chart: any | null = null;

  constructor(private svc: AdminDashboardService) {}

  ngOnChanges(changes: SimpleChanges): void {
    this.load();
  }

  async load() {
    try {
      const r: any = await this.svc.getRevenue(this.period, this.from || undefined, this.to || undefined, this.categoryId || undefined).toPromise();
      const labels = r?.labels || [];
      const values = r?.values || [];
      await this.render(labels, values);
    } catch (ex) { console.warn('Revenue chart load failed', ex); }
  }

  private async render(labels: string[], values: any[]) {
    const mod = await import('chart.js/auto');
    const ChartCtor = (mod as any).default ?? mod;
    const el = document.getElementById('revenueCanvasChild') as HTMLCanvasElement | null;
    if (!el) return;
    const ctx = el.getContext('2d');
    if (!ctx) return;
    try { this.chart?.destroy(); } catch {}
    this.chart = new ChartCtor(ctx, { type: 'line', data: { labels, datasets: [{ label: 'Revenue', data: values, borderColor: '#3f51b5', backgroundColor: 'rgba(63,81,181,0.12)', fill: true }] }, options: { responsive: true, maintainAspectRatio: false } });
  }
}
