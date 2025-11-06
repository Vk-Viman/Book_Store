import { Component } from '@angular/core';
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
      <h2 class="mb-4">
        <mat-icon class="me-2">dashboard</mat-icon>
        Admin Dashboard
      </h2>

      <div *ngIf="loading" class="text-center py-5">
        <mat-spinner></mat-spinner>
      </div>

      <div *ngIf="!loading" class="row">
        <div class="col-md-4 mb-4">
          <mat-card class="stat-card orders">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>receipt_long</mat-icon>
              </div>
              <div class="stat-value">{{ stats.totalOrders }}</div>
              <div class="stat-label">Total Orders</div>
            </mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-4 mb-4">
          <mat-card class="stat-card users">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>people</mat-icon>
              </div>
              <div class="stat-value">{{ stats.totalUsers }}</div>
              <div class="stat-label">Total Users</div>
            </mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-4 mb-4">
          <mat-card class="stat-card sales">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>attach_money</mat-icon>
              </div>
              <div class="stat-value">{{ stats.totalSales | currency }}</div>
              <div class="stat-label">Total Sales</div>
            </mat-card-content>
          </mat-card>
        </div>
      </div>

      <div *ngIf="!loading" class="row">
        <div class="col-md-12">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Top Products</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div *ngIf="topProducts.length === 0" class="text-muted">No sales data yet.</div>

              <canvas *ngIf="topProducts.length>0" id="topProductsChart"></canvas>

              <ul *ngIf="topProducts.length>0 && !showChart" class="list-unstyled">
                <li *ngFor="let p of topProducts">{{p.productName}} — {{p.quantitySold}}</li>
              </ul>

            </mat-card-content>
          </mat-card>
        </div>
      </div>

    </div>
  `,
  styles: [`
    canvas { width: 100% !important; height: 300px !important; }
    @media (max-width:600px) { .stat-value { font-size: 1.6rem; } }
  `]
})
export class AdminDashboardComponent {
  stats: DashboardStats = { totalUsers: 0, totalOrders: 0, totalSales: 0 };
  topProducts: TopProductDto[] = [];
  loading = true;

  showChart = true;

  constructor(private svc: AdminDashboardService) {
    this.load();
  }

  async load() {
    this.loading = true;
    this.svc.getStats().subscribe({ next: (s) => {
        this.stats = { totalUsers: s.totalUsers ?? 0, totalOrders: s.totalOrders ?? 0, totalSales: s.totalSales ?? 0 };
        this.svc.getTopProducts().subscribe({ next: async (t) => { this.topProducts = t || []; await this.renderChart(); this.loading = false; }, error: () => this.loading = false });
      }, error: () => { this.loading = false; } });
  }

  private async renderChart() {
    if (!this.topProducts || this.topProducts.length === 0) return;
    try {
      const Chart = (await import('chart.js/auto')).default;
      const ctx = (document.getElementById('topProductsChart') as HTMLCanvasElement).getContext('2d');
      const labels = this.topProducts.map(p => p.productName);
      const data = this.topProducts.map(p => p.quantitySold);
      new Chart(ctx!, {
        type: 'bar',
        data: { labels, datasets: [{ label: 'Quantity Sold', data, backgroundColor: '#3f51b5' }] },
        options: { responsive: true, maintainAspectRatio: false }
      });
    } catch (ex) {
      // fallback - don't break the UI
      this.showChart = false;
      console.warn('Chart render failed', ex);
    }
  }
}
