import { Component, ViewChild, ElementRef, ChangeDetectorRef, AfterViewInit, OnDestroy, NgZone } from '@angular/core';
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

              <div class="chart-wrap">
                <canvas #topCanvas id="topProductsChart"></canvas>
              </div>

            </mat-card-content>
          </mat-card>
        </div>
      </div>

    </div>
  `,
  styles: [
    `.stats-row { align-items: stretch; }
    .stat-card { flex: 1 1 220px; min-width: 200px; max-width: 360px; }
    .stat-card .stat-icon mat-icon { font-size: 36px; color: var(--primary-color); }
    .stat-value { font-size: 1.6rem; font-weight: 600; }
    .stat-label { font-size: 0.85rem; color: rgba(0,0,0,0.6); }
    .chart-wrap { position: relative; width: 100%; height: 300px; }
    canvas { width: 100% !important; height: 100% !important; display:block; }
    @media (max-width: 767px) { .stat-value { font-size: 1.25rem; } .stat-card { min-width: 140px; } .chart-wrap { height: 220px; } }
  `]
})
export class AdminDashboardComponent implements AfterViewInit, OnDestroy {
  stats: DashboardStats = { totalUsers: 0, totalOrders: 0, totalSales: 0 };
  topProducts: TopProductDto[] = [];
  loading = true;

  @ViewChild('topCanvas', { static: false }) topCanvas?: ElementRef<HTMLCanvasElement>;

  private chart: any | null = null;
  private resizeObs?: ResizeObserver;
  private ChartCtor: any | null = null;

  constructor(private svc: AdminDashboardService, private cd: ChangeDetectorRef, private zone: NgZone) {
    this.load();
  }

  ngAfterViewInit(): void {
    // Observe size; initialize chart once the canvas has non-zero size
    const canvas = this.topCanvas?.nativeElement;
    if (!canvas) return;
    this.resizeObs = new ResizeObserver(() => {
      const rect = canvas.getBoundingClientRect();
      if (rect.width > 0 && rect.height > 0) {
        this.initChartIfNeeded();
        if (this.chart && this.topProducts.length > 0) {
          this.updateChart();
        }
      }
    });
    this.resizeObs.observe(canvas);
  }

  ngOnDestroy(): void {
    try { this.resizeObs?.disconnect(); } catch {}
    try { this.chart?.destroy?.(); } catch {}
  }

  private async ensureChartCtor() {
    if (this.ChartCtor) return;
    const mod = await import('chart.js/auto');
    this.ChartCtor = (mod as any).default ?? mod;
  }

  private async initChartIfNeeded() {
    if (this.chart) return;
    const canvas = this.topCanvas?.nativeElement;
    if (!canvas) return;
    await this.ensureChartCtor();
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    this.chart = new this.ChartCtor(ctx, {
      type: 'bar',
      data: { labels: [], datasets: [{ label: 'Quantity Sold', data: [], backgroundColor: '#3f51b5' }] },
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
    });
  }

  private updateChart() {
    if (!this.chart) return;
    this.chart.data.labels = this.topProducts.map(p => p.productName);
    this.chart.data.datasets[0].data = this.topProducts.map(p => p.quantitySold);
    this.zone.runOutsideAngular(() => {
      this.chart.update();
    });
  }

  load() {
    this.loading = true;
    this.svc.getStats().subscribe({ next: (s) => {
        this.stats = { totalUsers: s.totalUsers ?? 0, totalOrders: s.totalOrders ?? 0, totalSales: s.totalSales ?? 0 };
        this.svc.getTopProducts().subscribe({ next: (t) => {
            this.topProducts = t || [];
            this.cd.detectChanges();
            // initialize or update chart after data arrives
            setTimeout(async () => { await this.initChartIfNeeded(); this.updateChart(); }, 0);
            this.loading = false;
          }, error: () => this.loading = false });
      }, error: () => { this.loading = false; } });
  }
}
