import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

interface DashboardStats {
  totalProducts: number;
  totalUsers: number;
  totalCategories: number;
  recentAudits: number;
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
        <div class="col-md-3 mb-4">
          <mat-card class="stat-card products">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>inventory_2</mat-icon>
              </div>
              <div class="stat-value">{{ stats.totalProducts }}</div>
              <div class="stat-label">Total Products</div>
            </mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-3 mb-4">
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

        <div class="col-md-3 mb-4">
          <mat-card class="stat-card categories">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>category</mat-icon>
              </div>
              <div class="stat-value">{{ stats.totalCategories }}</div>
              <div class="stat-label">Categories</div>
            </mat-card-content>
          </mat-card>
        </div>

        <div class="col-md-3 mb-4">
          <mat-card class="stat-card audits">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>history</mat-icon>
              </div>
              <div class="stat-value">{{ stats.recentAudits }}</div>
              <div class="stat-label">Recent Actions</div>
            </mat-card-content>
          </mat-card>
        </div>
      </div>

      <div *ngIf="!loading" class="row">
        <div class="col-md-12">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Quick Actions</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p>Admin dashboard with statistics and recent activity.</p>
              <p class="text-muted">More features coming soon: charts, recent orders, user activity, etc.</p>
            </mat-card-content>
          </mat-card>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .stat-card {
      text-align: center;
      transition: transform 0.2s;
      cursor: pointer;
    }
    .stat-card:hover {
      transform: translateY(-4px);
    }
    .stat-card.products { border-top: 4px solid #3f51b5; }
    .stat-card.users { border-top: 4px solid #4caf50; }
    .stat-card.categories { border-top: 4px solid #ff9800; }
    .stat-card.audits { border-top: 4px solid #f44336; }
    .stat-icon mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 8px;
    }
    .stat-card.products .stat-icon { color: #3f51b5; }
    .stat-card.users .stat-icon { color: #4caf50; }
    .stat-card.categories .stat-icon { color: #ff9800; }
    .stat-card.audits .stat-icon { color: #f44336; }
    .stat-value {
      font-size: 2.5rem;
      font-weight: 600;
      margin: 8px 0;
    }
    .stat-label {
      font-size: 0.875rem;
      color: var(--text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
  `]
})
export class AdminDashboardComponent {
  stats: DashboardStats = {
    totalProducts: 0,
    totalUsers: 0,
    totalCategories: 0,
    recentAudits: 0
  };
  loading = true;

  constructor(private http: HttpClient) {
    this.loadStats();
  }

  loadStats() {
    this.http.get<DashboardStats>('/api/admin/dashboard/stats').subscribe({
      next: (data) => {
        this.stats = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }
}
