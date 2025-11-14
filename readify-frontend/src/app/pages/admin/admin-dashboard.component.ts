import { Component, ChangeDetectorRef, AfterViewInit, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminDashboardService, TopProductDto } from '../../services/admin-dashboard.service';
import { NotificationService } from '../../services/notification.service';
import { FormsModule } from '@angular/forms';
import { MatGridListModule } from '@angular/material/grid-list';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, MatButtonModule, MatTooltipModule, MatGridListModule],
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

      <!-- Filters -->
      <div class="filters mb-3 d-flex gap-2 align-items-center">
        <label>From</label>
        <input type="date" [(ngModel)]="fromDate" />
        <label>To</label>
        <input type="date" [(ngModel)]="toDate" />
        <label>Category</label>
        <select [(ngModel)]="selectedCategory">
          <option [value]="0">All</option>
          <option *ngFor="let c of categories" [value]="c.id">{{c.name}}</option>
        </select>
        <button mat-stroked-button color="primary" (click)="applyFilters()">Apply</button>
      </div>

      <div *ngIf="!loading" class="row">
        <div class="col-12 mb-3">
          <mat-card>
            <mat-card-title>Top Products</mat-card-title>
            <mat-card-content>
              <div *ngIf="topProducts.length===0" class="text-muted py-3">No sales data yet.</div>
              <div *ngIf="topProducts.length>0">
                <button class="export-btn" title="Export CSV" (click)="exportTopProductsCsv()">⬇️ Export CSV</button>
              </div>
              <div class="chart-wrap mt-2">
                <canvas id="topProductsCanvas"></canvas>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-6">
          <mat-card class="chart-card">
            <mat-card-title>Revenue</mat-card-title>
            <mat-card-content><div class="chart-wrap"><canvas id="revenueCanvas"></canvas></div></mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-6">
          <mat-card class="chart-card">
            <mat-card-title>Top Categories</mat-card-title>
            <mat-card-content><div class="chart-wrap"><canvas id="topCategoriesCanvas"></canvas></div></mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-6">
          <mat-card class="chart-card">
            <mat-card-title>Top Authors</mat-card-title>
            <mat-card-content><div class="chart-wrap"><canvas id="topAuthorsCanvas"></canvas></div></mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-6">
          <mat-card class="chart-card">
            <mat-card-title>User Registrations</mat-card-title>
            <mat-card-content><div class="chart-wrap"><canvas id="usersCanvas"></canvas></div></mat-card-content>
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
    .chart-card { position: relative; }
    .floating-toolbar { position: absolute; top: 12px; right: 12px; z-index:1000; display:flex; gap:8px; }
    .export-btn { background:#1e88e5; color:#fff; border:none; padding:8px 12px; border-radius:6px; cursor:pointer; font-weight:600; box-shadow:0 1px 2px rgba(0,0,0,0.05); }
    .export-btn.secondary { background:#6c757d; }
    .filters { margin-bottom: 12px; }
    @media (max-width: 767px) { .chart-wrap { height: 220px; } }
  `]
})
export class AdminDashboardComponent implements AfterViewInit, OnDestroy {
  stats = { totalUsers: 0, totalOrders: 0, totalSales: 0 };
  topProducts: TopProductDto[] = [];
  loading = true;

  fromDate: string | null = null;
  toDate: string | null = null;
  categories: any[] = [];
  selectedCategory = 0;
  period = 30;

  private ChartCtor: any | null = null;
  private charts = new Map<string, any>();
  public topChart: any | null = null;

  // debug flag to log API payloads when investigating totals
  private debug = false;

  constructor(private svc: AdminDashboardService, private cd: ChangeDetectorRef, private zone: NgZone, private notify: NotificationService, private prodSvc: ProductService) {
    this.load();
  }

  ngAfterViewInit(): void {
    // after view init, render charts if data already fetched
    setTimeout(() => this.loadCharts(), 0);
  }

  ngOnDestroy(): void { try { this.charts.forEach(c => c?.destroy?.()); } catch {} }

  private async ensureChartCtor() {
    if (this.ChartCtor) return;
    const mod = await import('chart.js/auto');
    this.ChartCtor = (mod as any).default ?? mod;
  }

  private async createChart(id: string, cfg: any) {
    await this.ensureChartCtor();
    const el = document.getElementById(id) as HTMLCanvasElement | null;
    if (!el) return null;
    const ctx = el.getContext('2d');
    if (!ctx) return null;
    // destroy existing
    const existing = this.charts.get(id);
    if (existing) try { existing.destroy(); } catch {}
    const chart = new this.ChartCtor(ctx, cfg);
    this.charts.set(id, chart);
    return chart;
  }

  private async renderTopProductsCanvas() {
    // render or update top products chart canvas
    if (!this.topProducts || this.topProducts.length === 0) return;
    const cfgTop = {
      type: 'bar',
      data: {
        labels: this.topProducts.map(p => p.productName),
        datasets: [{ label: 'Qty', data: this.topProducts.map(p => p.quantitySold), backgroundColor: '#3f51b5' }]
      },
      options: { responsive: true, maintainAspectRatio: false }
    };
    try {
      this.topChart = await this.createChart('topProductsCanvas', cfgTop);
    } catch (ex) {
      console.warn('Failed to render top products canvas', ex);
    }
  }

  private async loadCharts() {
    // top products
    try {
      const t: any = await this.svc.getTopProducts().toPromise();
      if (this.debug) console.debug('top-products', t);
      this.topProducts = t || [];
      const cfgTop = { type: 'bar', data: { labels: this.topProducts.map(p => p.productName), datasets: [{ label: 'Qty', data: this.topProducts.map(p => p.quantitySold), backgroundColor: '#3f51b5' }] }, options: { responsive: true, maintainAspectRatio: false } };
      this.topChart = await this.createChart('topProductsCanvas', cfgTop);
    } catch (ex) { console.warn('Failed to load top products', ex); }

    // revenue
    try {
      const r: any = await this.svc.getRevenue(this.period, this.fromDate || undefined, this.toDate || undefined, this.selectedCategory || undefined).toPromise();
      if (this.debug) console.debug('revenue', r);
      const labels = r?.labels || [];
      const values = r?.values || [];
      // determine totalRevenue from possible property names, fall back to sum of values
      const apiTotal = (r && (r.totalRevenue ?? r.TotalRevenue ?? r.Total)) ?? null;
      const fallbackTotal = values.reduce((s: number, v: any) => s + Number(v || 0), 0);
      const totalFromApi = apiTotal != null ? Number(apiTotal) : fallbackTotal;
      // normalize to two decimals to avoid floating point display mismatch
      this.stats.totalSales = Math.round((totalFromApi + Number.EPSILON) * 100) / 100;

      const cfg = { type: 'line', data: { labels, datasets: [{ label: 'Revenue', data: values, borderColor: '#3f51b5', backgroundColor: 'rgba(63,81,181,0.12)', fill: true }] }, options: { responsive: true, maintainAspectRatio: false } };
      await this.createChart('revenueCanvas', cfg);
    } catch (ex) { console.warn('Failed to load revenue', ex); }

    // top categories
    try {
      const c: any = await this.svc.getTopCategories(8, this.fromDate || undefined, this.toDate || undefined).toPromise();
      const labels = c?.labels || [];
      const values = c?.values || [];
      const cfg = { type: 'bar', data: { labels, datasets: [{ label: 'Revenue', data: values, backgroundColor: '#4caf50' }] }, options: { responsive: true, maintainAspectRatio: false } };
      await this.createChart('topCategoriesCanvas', cfg);
    } catch (ex) { console.warn('Failed to load top categories', ex); }

    // top authors
    try {
      const a: any = await this.svc.getTopAuthors(8, this.fromDate || undefined, this.toDate || undefined).toPromise();
      const labels = a?.labels || [];
      const values = a?.values || [];
      const cfg = { type: 'bar', data: { labels, datasets: [{ label: 'Revenue', data: values, backgroundColor: '#ff9800' }] }, options: { responsive: true, maintainAspectRatio: false } };
      await this.createChart('topAuthorsCanvas', cfg);
    } catch (ex) { console.warn('Failed to load top authors', ex); }

    // users
    try {
      const u: any = await this.svc.getUserTrend(this.period, this.fromDate || undefined, this.toDate || undefined).toPromise();
      const labels = u?.labels || [];
      const values = u?.values || [];
      const cfg = { type: 'line', data: { labels, datasets: [{ label: 'Users', data: values, borderColor: '#009688', backgroundColor: 'rgba(0,150,136,0.12)', fill: true }] }, options: { responsive: true, maintainAspectRatio: false } };
      await this.createChart('usersCanvas', cfg);
    } catch (ex) { console.warn('Failed to load users', ex); }
  }

  load() {
    this.loading = true;
    // call filtered summary (if filters provided)
    this.svc.getSummary(this.fromDate || undefined, this.toDate || undefined, this.selectedCategory || undefined).subscribe({ next: (s: any) => {
        // s should contain TotalUsers/TotalOrders/TotalRevenue (case may vary)
        this.stats.totalUsers = s?.totalUsers ?? s?.TotalUsers ?? this.stats.totalUsers;
        this.stats.totalOrders = s?.totalOrders ?? s?.TotalOrders ?? this.stats.totalOrders;
        // set totalSales from filtered summary (ensure numeric and rounded)
        const apiSales = s?.totalRevenue ?? s?.TotalRevenue ?? s?.totalSales ?? s?.TotalSales;
        if (apiSales != null) {
          this.stats.totalSales = Math.round((Number(apiSales) + Number.EPSILON) * 100) / 100;
        }

        // load categories
        this.prodSvc.getCategories().subscribe({ next: (cats: any) => { this.categories = cats || []; }, error: () => { this.categories = []; } });

        // load top products
        this.svc.getTopProducts().subscribe({ next: (t) => { this.topProducts = t || []; this.cd.detectChanges(); setTimeout(() => this.renderTopProductsCanvas(), 0); }, error: () => {} });

        // load charts
        setTimeout(() => this.loadCharts(), 0);
        this.loading = false;
      }, error: () => { this.loading = false; } });
  }

  applyFilters() {
    // reload summary and charts using the selected filters
    this.load();
  }

  exportTopProductsCsv() {
    if (!this.topProducts || this.topProducts.length === 0) return;
    const rows = [['Product','Quantity']];
    for (const p of this.topProducts) rows.push([p.productName, String(p.quantitySold)]);
    const csv = rows.map(r => r.map(c => '"' + String(c).replace(/"/g,'""') + '"').join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `top-products-${new Date().toISOString().slice(0,19).replace(/[:T]/g,'-')}.csv`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
  }

  exportChartPng(canvasId: string) {
    const el = document.getElementById(canvasId) as HTMLCanvasElement | null;
    if (!el) return;
    try {
      const url = el.toDataURL('image/png');
      const a = document.createElement('a');
      a.href = url;
      a.download = `${canvasId}-${new Date().toISOString().slice(0,19).replace(/[:T]/g,'-')}.png`;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } catch (ex) { console.warn('Export PNG failed', ex); }
  }

}
