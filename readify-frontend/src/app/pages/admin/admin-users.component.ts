import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatCardModule],
  template: `
    <div class="container mt-4">
      <mat-card>
        <mat-card-title>Users</mat-card-title>
        <mat-card-content>
          <table mat-table [dataSource]="users" class="mat-elevation-z8" *ngIf="users.length>0">
            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>Id</th>
              <td mat-cell *matCellDef="let u">{{u.id}}</td>
            </ng-container>
            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef>Email</th>
              <td mat-cell *matCellDef="let u">{{u.email}}</td>
            </ng-container>
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let u">{{u.fullName}}</td>
            </ng-container>
            <ng-container matColumnDef="role">
              <th mat-header-cell *matHeaderCellDef>Role</th>
              <td mat-cell *matCellDef="let u">{{u.role}}</td>
            </ng-container>
            <ng-container matColumnDef="active">
              <th mat-header-cell *matHeaderCellDef>Active</th>
              <td mat-cell *matCellDef="let u">{{u.isActive ? 'Yes' : 'No'}}</td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let u">
                <button mat-button color="warn" (click)="toggleActive(u)">{{u.isActive ? 'Deactivate' : 'Activate'}}</button>
                <button mat-button color="primary" (click)="promote(u)">Promote to Admin</button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <div *ngIf="users.length===0" class="text-center py-4">No users found.</div>
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class AdminUsersComponent {
  users: any[] = [];
  displayedColumns = ['id', 'email', 'name', 'role', 'active', 'actions'];

  constructor(private http: HttpClient) { this.load(); }

  load() { this.http.get<any[]>('/api/admin/users').subscribe({ next: (r: any[]) => this.users = r, error: () => this.users = [] }); }

  toggleActive(u: any) {
    this.http.put(`/api/admin/users/${u.id}/toggle-active`, {}).subscribe({ next: () => this.load(), error: () => alert('Failed') });
  }

  promote(u: any) {
    this.http.put(`/api/admin/users/${u.id}/promote`, {}).subscribe({ next: () => this.load(), error: () => alert('Failed') });
  }
}
