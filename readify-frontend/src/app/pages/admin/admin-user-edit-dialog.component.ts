import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-admin-user-edit-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, FormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Edit User</h2>
    <mat-dialog-content>
      <div class="mb-3">
        <mat-form-field appearance="outline" class="w-100">
          <mat-label>Full name</mat-label>
          <input matInput [(ngModel)]="data.fullName" />
        </mat-form-field>
      </div>
      <div class="mb-3">
        <mat-form-field appearance="outline" class="w-100">
          <mat-label>Email</mat-label>
          <input matInput [(ngModel)]="data.email" type="email" />
        </mat-form-field>
      </div>
      <div class="mb-3">
        <mat-form-field appearance="outline" class="w-100">
          <mat-label>Role</mat-label>
          <mat-select [(ngModel)]="data.role">
            <mat-option value="User">User</mat-option>
            <mat-option value="Admin">Admin</mat-option>
          </mat-select>
        </mat-form-field>
      </div>
      <div>
        <mat-checkbox [(ngModel)]="data.isActive">Active</mat-checkbox>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" (click)="save()">Save</button>
    </mat-dialog-actions>
  `
})
export class AdminUserEditDialogComponent {
  constructor(public dialogRef: MatDialogRef<AdminUserEditDialogComponent>, @Inject(MAT_DIALOG_DATA) public data: any) {}
  save() { this.dialogRef.close(this.data); }
}
