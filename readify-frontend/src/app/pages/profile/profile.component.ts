import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
  <div class="container mt-4" style="max-width: 560px;">
    <h3>Profile</h3>

    <form [formGroup]="profileForm" (ngSubmit)="saveProfile()" class="mb-4">
      <div class="mb-3">
        <label>Full Name</label>
        <input class="form-control" formControlName="fullName">
      </div>
      <div class="mb-3">
        <label>Email</label>
        <input class="form-control" formControlName="email" type="email">
      </div>
      <button class="btn btn-primary" [disabled]="profileForm.invalid || saving">{{ saving ? 'Saving...' : 'Save profile' }}</button>
    </form>

    <h4>Change Password</h4>
    <form [formGroup]="passwordForm" (ngSubmit)="changePassword()">
      <div class="mb-3">
        <label>Current password</label>
        <input class="form-control" formControlName="currentPassword" type="password">
      </div>
      <div class="mb-3">
        <label>New password</label>
        <input class="form-control" formControlName="newPassword" type="password">
      </div>
      <button class="btn btn-outline-secondary" [disabled]="passwordForm.invalid || changing">{{ changing ? 'Updating...' : 'Change password' }}</button>
    </form>
  </div>
  `
})
export class ProfileComponent {
  profileForm: FormGroup;
  passwordForm: FormGroup;
  saving = false;
  changing = false;

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
      next: () => { this.saving = false; this.notify.success('Profile updated'); },
      error: (e) => { this.saving = false; this.notify.error(e?.error?.message || 'Failed to update profile'); }
    });
  }

  changePassword() {
    if (this.passwordForm.invalid) return;
    this.changing = true;
    this.http.put('/api/users/change-password', this.passwordForm.value).subscribe({
      next: () => { this.changing = false; this.passwordForm.reset(); this.notify.success('Password changed'); },
      error: (e) => { this.changing = false; this.notify.error(e?.error?.message || 'Failed to change password'); }
    });
  }
}
