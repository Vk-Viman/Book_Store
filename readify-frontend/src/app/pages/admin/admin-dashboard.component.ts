import { Component, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminDashboardService, TopProductDto } from './admin-dashboard.service';

interface DashboardStats {
  totalUsers: number;
  totalOrders: number;
  totalSales: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="container mt-4">
      <h2 class="mb-4 d-flex align-items-center">
        <mat-icon class="me-2">dashboard</mat-icon>
        Admin Dashboard
      </h2>

      <div *ngIf="loading" class="text-center py-5">
        <mat-spinner></mat-spinner>
      </div>

      <div *ngIf="!loading" class="stats-row d-flex flex-wrap gap-3 mb-3">
        <mat-card class="stat-card">
          <mat-card-content class="d-flex align-items-center gap-3">
            <div class="stat-icon"><mat-icon>receipt_long</mat-icon></div>
            <div>
              <div class="stat-value">{{ stats.totalOrders }}</div>
              <div class="stat-label">Total Orders</div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-content class="d-flex align-items-center gap-3">
            <div class="stat-icon"><mat-icon>people</mat-icon></div>
            <div>
              <div class="stat-value">{{ stats.totalUsers }}</div>
              <div class="stat-label">Total Users</div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="stat-card">
          <mat-card-content class="d-flex align-items-center gap-3">
            <div class="stat-icon"><mat-icon>attach_money</mat-icon></div>
            <div>
              <div class="stat-value">{{ stats.totalSales | currency }}</div>
              <div class="stat-label">Total Sales</div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <div *ngIf="!loading" class="row">
        <div class="col-12">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Top Products</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div *ngIf="topProducts.length === 0" class="text-muted py-3">No sales data yet.</div>

              <div class="chart-wrap" *ngIf="topProducts.length>0">
                <canvas #topCanvas id="topProductsChart"></canvas>
              </div>

              <ul *ngIf="topProducts.length>0 && !showChart" class="list-unstyled mt-2">
                <li *ngFor="let p of topProducts">{{p.productName}} — {{p.quantitySold}}</li>
              </ul>

            </mat-card-content>
          </mat-card>
        </div>
      </div>

    </div>
  `,
  styles: [`
    .stats-row { align-items: stretch; }
    .stat-card { flex: 1 1 220px; min-width: 200px; max-width: 360px; }
    .stat-card .stat-icon mat-icon { font-size: 36px; color: var(--primary-color); }
    .stat-value { font-size: 1.6rem; font-weight: 600; }
    .stat-label { font-size: 0.85rem; color: rgba(0,0,0,0.6); }
    .chart-wrap { position: relative; width: 100%; max-height: 360px; }
    canvas { width: 100% !important; height: 300px !important; }

    @media (max-width: 767px) {
      .stat-value { font-size: 1.25rem; }
      .stat-card { min-width: 140px; }
      canvas { height: 220px !important; }
    }
  `]
})
export class AdminDashboardComponent {
  stats: DashboardStats = { totalUsers: 0, totalOrders: 0, totalSales: 0 };
  topProducts: TopProductDto[] = [];
  loading = true;

  showChart = true;

  @ViewChild('topCanvas', { static: false }) topCanvas?: ElementRef<HTMLCanvasElement>;

  constructor(private svc: AdminDashboardService, private cd: ChangeDetectorRef) {
    this.load();
  }

  async load() {
    this.loading = true;
    this.svc.getStats().subscribe({ next: (s) => {
        this.stats = { totalUsers: s.totalUsers ?? 0, totalOrders: s.totalOrders ?? 0, totalSales: s.totalSales ?? 0 };
        this.svc.getTopProducts().subscribe({ next: async (t) => { this.topProducts = t || []; /* allow view to update */ this.cd.detectChanges(); await new Promise(r => setTimeout(r, 0)); await this.renderChart(); this.loading = false; }, error: () => this.loading = false });
      }, error: () => { this.loading = false; } });
  }

  private async renderChart() {
    if (!this.topProducts || this.topProducts.length === 0) return;
    try {
      const ChartModule = await import('chart.js/auto');
      const Chart = (ChartModule as any).default ?? ChartModule;

      // ensure canvas element is present
      const canvasEl: HTMLCanvasElement | undefined = this.topCanvas?.nativeElement ?? document.getElementById('topProductsChart') as HTMLCanvasElement | null ?? undefined;
      if (!canvasEl) {
        // If canvas not found, bail and fallback to list
        this.showChart = false;
        console.warn('Chart canvas not found');
        return;
      }

      const ctx = canvasEl.getContext('2d');
      if (!ctx) {
        this.showChart = false;
        console.warn('Unable to get canvas context');
        return;
      }

      const labels = this.topProducts.map(p => p.productName);
      const data = this.topProducts.map(p => p.quantitySold);
      // destroy previous chart instance if present
      (canvasEl as any).__chartInstance?.destroy?.();

      const chart = new (Chart as any)(ctx, {
        type: 'bar',
        data: { labels, datasets: [{ label: 'Quantity Sold', data, backgroundColor: '#3f51b5' }] },
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
      });

      // store instance to allow cleanup
      (canvasEl as any).__chartInstance = chart;
    } catch (ex) {
      this.showChart = false;
      console.warn('Chart render failed', ex);
    }
  }
}
