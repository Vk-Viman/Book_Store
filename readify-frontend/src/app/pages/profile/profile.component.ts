import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
  <div class="container mt-4" style="max-width: 720px;">
    <mat-card class="mb-4">
      <mat-card-header>
        <mat-card-title>
          <mat-icon>person</mat-icon>
          Profile Information
        </mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="profileForm" (ngSubmit)="saveProfile()">
          <mat-form-field appearance="outline" class="w-100 mb-3">
            <mat-label>Full Name</mat-label>
            <input matInput formControlName="fullName" required>
            <mat-icon matPrefix>badge</mat-icon>
            <mat-error *ngIf="profileForm.get('fullName')?.hasError('required')">
              Full name is required
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-100 mb-3">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" required>
            <mat-icon matPrefix>email</mat-icon>
            <mat-error *ngIf="profileForm.get('email')?.hasError('required')">
              Email is required
            </mat-error>
            <mat-error *ngIf="profileForm.get('email')?.hasError('email')">
              Please enter a valid email
            </mat-error>
          </mat-form-field>

          <div class="d-flex justify-content-end">
            <button mat-raised-button color="primary" type="submit" [disabled]="profileForm.invalid || saving">
              <mat-icon *ngIf="!saving">save</mat-icon>
              <mat-spinner *ngIf="saving" diameter="20" class="me-2"></mat-spinner>
              {{ saving ? 'Saving...' : 'Save Profile' }}
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>

    <mat-card>
      <mat-card-header>
        <mat-card-title>
          <mat-icon>lock</mat-icon>
          Change Password
        </mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="passwordForm" (ngSubmit)="changePassword()">
          <mat-form-field appearance="outline" class="w-100 mb-3">
            <mat-label>Current Password</mat-label>
            <input matInput [type]="hideCurrentPassword ? 'password' : 'text'" formControlName="currentPassword" required>
            <mat-icon matPrefix>lock_outline</mat-icon>
            <button mat-icon-button matSuffix (click)="hideCurrentPassword = !hideCurrentPassword" type="button" [attr.aria-label]="'Toggle password visibility'">
              <mat-icon>{{hideCurrentPassword ? 'visibility_off' : 'visibility'}}</mat-icon>
            </button>
            <mat-error *ngIf="passwordForm.get('currentPassword')?.hasError('required')">
              Current password is required
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-100 mb-3">
            <mat-label>New Password</mat-label>
            <input matInput [type]="hideNewPassword ? 'password' : 'text'" formControlName="newPassword" required>
            <mat-icon matPrefix>lock</mat-icon>
            <button mat-icon-button matSuffix (click)="hideNewPassword = !hideNewPassword" type="button" [attr.aria-label]="'Toggle password visibility'">
              <mat-icon>{{hideNewPassword ? 'visibility_off' : 'visibility'}}</mat-icon>
            </button>
            <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('required')">
              New password is required
            </mat-error>
            <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('minlength')">
              Password must be at least 8 characters
            </mat-error>
            <mat-hint>Minimum 8 characters</mat-hint>
          </mat-form-field>

          <div class="d-flex justify-content-end">
            <button mat-raised-button color="accent" type="submit" [disabled]="passwordForm.invalid || changing">
              <mat-icon *ngIf="!changing">vpn_key</mat-icon>
              <mat-spinner *ngIf="changing" diameter="20" class="me-2"></mat-spinner>
              {{ changing ? 'Updating...' : 'Change Password' }}
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  </div>
  `,
  styles: [`
    mat-card {
      margin-bottom: 24px;
    }
    mat-card-title {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    mat-spinner {
      display: inline-block;
    }
  `]
})
export class ProfileComponent {
  profileForm: FormGroup;
  passwordForm: FormGroup;
  saving = false;
  changing = false;
  hideCurrentPassword = true;
  hideNewPassword = true;

  constructor(private fb: FormBuilder, private http: HttpClient, private notify: NotificationService) {
    this.profileForm = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });
    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]]
    });
    this.load();
  }

  load() {
    this.http.get('/api/users/me').subscribe({
      next: (res: any) => {
        this.profileForm.patchValue({ fullName: res?.fullName ?? res?.FullName ?? '', email: res?.email ?? res?.Email ?? '' });
      },
      error: () => this.notify.error('Failed to load profile')
    });
  }

  saveProfile() {
    if (this.profileForm.invalid) return;
    this.saving = true;
    this.http.put('/api/users/me', this.profileForm.value).subscribe({
      next: () => { this.saving = false; this.notify.success('Profile updated successfully'); },
      error: (e) => { this.saving = false; this.notify.error(e?.error?.message || 'Failed to update profile'); }
    });
  }

  changePassword() {
    if (this.passwordForm.invalid) return;
    this.changing = true;
    this.http.put('/api/users/change-password', this.passwordForm.value).subscribe({
      next: () => { this.changing = false; this.passwordForm.reset(); this.notify.success('Password changed successfully'); },
      error: (e) => { this.changing = false; this.notify.error(e?.error?.message || 'Failed to change password'); }
    });
  }
}
