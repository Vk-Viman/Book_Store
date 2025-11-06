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

              <!-- Simple, reliable bar chart built with CSS -->
              <div *ngIf="topProducts.length>0" class="fallback-bars">
                <div *ngFor="let p of topProducts" class="bar-row">
                  <div class="bar-label">{{p.productName}}</div>
                  <div class="bar-track">
                    <div class="bar-fill" [style.width.%]="(p.quantitySold / maxQuantity) * 100"></div>
                    <span class="bar-value">{{p.quantitySold}}</span>
                  </div>
                </div>
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

    /* CSS bars */
    .fallback-bars { padding: 8px 4px 4px; }
    .bar-row { display: flex; align-items: center; gap: 8px; margin: 12px 0; }
    .bar-label { width: 180px; min-width: 120px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .bar-track { position: relative; flex: 1; height: 12px; background: #e9ecef; border-radius: 6px; }
    .bar-fill { position: absolute; left: 0; top: 0; bottom: 0; background: #3f51b5; border-radius: 6px; }
    .bar-value { position: absolute; right: 6px; top: -18px; font-size: 12px; color: rgba(0,0,0,0.7); }

    @media (max-width: 767px) {
      .stat-value { font-size: 1.25rem; }
      .stat-card { min-width: 140px; }
      .bar-label { width: 120px; }
    }
  `]
})
export class AdminDashboardComponent {
  stats: DashboardStats = { totalUsers: 0, totalOrders: 0, totalSales: 0 };
  topProducts: TopProductDto[] = [];
  loading = true;
  maxQuantity = 1;

  constructor(private svc: AdminDashboardService) { this.load(); }

  load() {
    this.loading = true;
    this.svc.getStats().subscribe({ next: (s) => {
        this.stats = { totalUsers: s.totalUsers ?? 0, totalOrders: s.totalOrders ?? 0, totalSales: s.totalSales ?? 0 };
        this.svc.getTopProducts().subscribe({ next: (t) => { this.topProducts = t || []; this.maxQuantity = Math.max(...this.topProducts.map(p => p.quantitySold), 1); this.loading = false; }, error: () => this.loading = false });
      }, error: () => { this.loading = false; } });
  }
}
