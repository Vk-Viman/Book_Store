import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { NotificationService } from '../../services/notification.service';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatCardModule, MatInputModule, FormsModule, MatPaginatorModule, MatCheckboxModule],
  template: `
    <div class="container mt-4">
      <mat-card>
        <mat-card-title class="d-flex align-items-center">
          <span>Pending Reviews</span>
          <span class="ms-auto small text-muted">Showing page {{page}} of {{totalPages}}</span>
        </mat-card-title>
        <mat-card-content>
          <div class="d-flex mb-3 gap-2">
            <input matInput placeholder="Search comments" [(ngModel)]="q" (keyup.enter)="load()" />
            <button mat-stroked-button (click)="load()">Search</button>
            <select class="form-select w-auto" [(ngModel)]="pageSize" (change)="onPageSizeChange($event)">
              <option [value]="10">10</option>
              <option [value]="25">25</option>
              <option [value]="50">50</option>
              <option [value]="100">100</option>
            </select>
            <span class="ms-auto d-flex gap-2">
              <button mat-raised-button color="primary" (click)="bulkApprove()" [disabled]="selectedIds.size===0 || bulkLoading">Approve selected</button>
              <button mat-raised-button color="warn" (click)="bulkReject()" [disabled]="selectedIds.size===0 || bulkLoading">Reject selected</button>
            </span>
          </div>

          <div class="mb-2"><small class="text-muted">Total pending: {{ total }}</small></div>

          <div *ngIf="loading" class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>

          <table mat-table [dataSource]="reviews" class="w-100" *ngIf="!loading">
            <ng-container matColumnDef="select">
              <th mat-header-cell *matHeaderCellDef>
                <mat-checkbox [checked]="allSelected" (change)="toggleSelectAll($event.checked)"></mat-checkbox>
              </th>
              <td mat-cell *matCellDef="let r">
                <mat-checkbox [checked]="selectedIds.has(r.id)" (change)="toggleSelection(r.id, $event.checked)"></mat-checkbox>
              </td>
            </ng-container>

            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>ID</th>
              <td mat-cell *matCellDef="let r">{{r.id}}</td>
            </ng-container>
            <ng-container matColumnDef="product">
              <th mat-header-cell *matHeaderCellDef>Product</th>
              <td mat-cell *matCellDef="let r">{{r.productId}}</td>
            </ng-container>
            <ng-container matColumnDef="user">
              <th mat-header-cell *matHeaderCellDef>User</th>
              <td mat-cell *matCellDef="let r">{{r.userId}}</td>
            </ng-container>
            <ng-container matColumnDef="rating">
              <th mat-header-cell *matHeaderCellDef>Rating</th>
              <td mat-cell *matCellDef="let r">{{r.rating}}</td>
            </ng-container>
            <ng-container matColumnDef="comment">
              <th mat-header-cell *matHeaderCellDef>Comment</th>
              <td mat-cell *matCellDef="let r">{{r.comment}}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let r">
                <button mat-button color="primary" (click)="approve(r.id)" [disabled]="bulkLoading">Approve</button>
                <button mat-button color="warn" (click)="reject(r.id)" [disabled]="bulkLoading">Reject</button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <div class="paginator-wrapper mt-3 text-end">
            <mat-paginator [length]="total" [pageSize]="pageSize" [pageIndex]="page-1" (page)="pageChange($event)"></mat-paginator>
            <div class="text-muted small mt-2">Page {{page}} of {{ totalPages }}</div>
          </div>

        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [``]
})
export class AdminReviewsComponent {
  reviews: any[] = [];
  displayedColumns = ['select','id','product','user','rating','comment','actions'];
  q = '';
  page = 1;
  pageSize = 10;
  total = 0;
  totalPages = 0;

  selectedIds = new Set<number>();
  allSelected = false;

  loading = false;
  bulkLoading = false;

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(private http: HttpClient, private notify: NotificationService) { this.load(); }

  load() {
    this.loading = true;
    const q = this.q ? `&q=${encodeURIComponent(this.q)}` : '';
    this.http.get<any>(`/api/admin/reviews?page=${this.page}&pageSize=${this.pageSize}${q}`).subscribe({ next: (r: any) => { this.reviews = r.items ?? []; this.total = r.total ?? 0; this.page = r.page ?? this.page; this.pageSize = r.pageSize ?? this.pageSize; this.totalPages = Math.ceil((this.total || 0) / this.pageSize); this.syncSelection(); this.loading = false; }, error: () => { this.reviews = []; this.total = 0; this.totalPages = 0; this.loading = false; } });
  }

  approve(id: number) { this.bulkLoading = true; this.http.put(`/api/admin/reviews/${id}/approve`, true).subscribe({ next: () => { this.notify.success('Approved'); this.load(); this.bulkLoading = false; }, error: () => { this.notify.error('Failed'); this.bulkLoading = false; } }); }

  reject(id: number) { this.bulkLoading = true; this.http.put(`/api/admin/reviews/${id}/approve`, false).subscribe({ next: () => { this.notify.success('Rejected'); this.load(); this.bulkLoading = false; }, error: () => { this.notify.error('Failed'); this.bulkLoading = false; } }); }

  toggleSelection(id: number, checked: boolean) {
    if (checked) this.selectedIds.add(id); else this.selectedIds.delete(id);
    this.allSelected = this.reviews.length > 0 && this.reviews.every(r => this.selectedIds.has(r.id));
  }

  toggleSelectAll(checked: boolean) {
    if (checked) {
      this.reviews.forEach(r => this.selectedIds.add(r.id));
    } else {
      this.reviews.forEach(r => this.selectedIds.delete(r.id));
    }
    this.allSelected = checked;
  }

  syncSelection() {
    // remove selections not on current page
    const pageIds = new Set(this.reviews.map(r => r.id));
    Array.from(this.selectedIds).forEach(id => { if (!pageIds.has(id)) this.selectedIds.delete(id); });
    this.allSelected = this.reviews.length > 0 && this.reviews.every(r => this.selectedIds.has(r.id));
  }

  bulkApprove() {
    if (this.selectedIds.size === 0) return;
    this.bulkLoading = true;
    const ids = Array.from(this.selectedIds);
    this.http.post(`/api/admin/reviews/bulk`, { ids, approve: true }).subscribe({ next: () => { this.notify.success('Approved selected'); this.selectedIds.clear(); this.load(); this.bulkLoading = false; }, error: () => { this.notify.error('Failed to approve selected'); this.bulkLoading = false; } });
  }

  bulkReject() {
    if (this.selectedIds.size === 0) return;
    this.bulkLoading = true;
    const ids = Array.from(this.selectedIds);
    this.http.post(`/api/admin/reviews/bulk`, { ids, approve: false }).subscribe({ next: () => { this.notify.success('Rejected selected'); this.selectedIds.clear(); this.load(); this.bulkLoading = false; }, error: () => { this.notify.error('Failed to reject selected'); this.bulkLoading = false; } });
  }

  onPageSizeChange(ev: any) {
    this.page = 1;
    this.load();
  }

  pageChange(ev: PageEvent) {
    this.page = ev.pageIndex + 1;
    this.pageSize = ev.pageSize;
    this.load();
  }
}
