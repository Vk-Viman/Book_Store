import { Component } from '@angular/core';
import { FormBuilder, Validators, FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
  <div class="container mt-5" style="max-width: 450px;">
    <h3 class="text-center mb-4">Forgot Password</h3>
    <form (ngSubmit)="submit()">
      <div class="form-group mb-3">
        <label>Email</label>
        <input type="email" class="form-control" [(ngModel)]="email" name="email" required>
      </div>
      <button class="btn btn-primary w-100" [disabled]="loading">Send reset link</button>
    </form>
    <div *ngIf="message" class="alert alert-success mt-3">{{ message }}</div>
    <div *ngIf="error" class="alert alert-danger mt-3">{{ error }}</div>
    <p class="mt-3 text-center">
      <a routerLink="/login">Back to login</a>
    </p>
  </div>
  `
})
export class ForgotPasswordComponent {
  email = '';
  loading = false;
  message = '';
  error = '';

  constructor(private http: HttpClient) {}

  submit() {
    this.loading = true;
    this.error = '';
    this.http.post(`/api/auth/forgot-password`, { email: this.email }).subscribe({
      next: (res: any) => { this.message = res?.message ?? 'If the email exists, a reset link has been sent.'; this.loading = false; },
      error: (err) => { this.error = err?.error?.message ?? 'Request failed'; this.loading = false; }
    });
  }
}
