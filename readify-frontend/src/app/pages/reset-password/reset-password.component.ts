import { Component } from '@angular/core';
import { FormBuilder, Validators, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
  <div class="container mt-5" style="max-width: 450px;">
    <h3 class="text-center mb-4">Reset Password</h3>
    <form (ngSubmit)="submit()">
      <div class="form-group mb-3">
        <label>New password</label>
        <input type="password" class="form-control" [(ngModel)]="password" name="password" required>
      </div>
      <div class="form-group mb-3">
        <label>Confirm password</label>
        <input type="password" class="form-control" [(ngModel)]="confirm" name="confirm" required>
      </div>
      <button class="btn btn-primary w-100" [disabled]="loading">Reset password</button>
    </form>
    <div *ngIf="message" class="alert alert-success mt-3">{{ message }}</div>
    <div *ngIf="error" class="alert alert-danger mt-3">{{ error }}</div>
    <p class="mt-3 text-center">
      <a routerLink="/login">Back to login</a>
    </p>
  </div>
  `
})
export class ResetPasswordComponent {
  password = '';
  confirm = '';
  loading = false;
  message = '';
  error = '';
  token = '';
  private api = environment.apiUrl;

  constructor(private route: ActivatedRoute, private http: HttpClient, private router: Router) {
    this.token = this.route.snapshot.paramMap.get('token') ?? '';
  }

  submit() {
    this.error = '';
    if (this.password !== this.confirm) { this.error = 'Passwords do not match'; return; }
    this.loading = true;
    this.http.post(`${this.api}/auth/reset-password`, { token: this.token, newPassword: this.password }).subscribe({
      next: (res: any) => {
        this.message = res?.message ?? 'Password reset';
        this.loading = false;
        setTimeout(() => this.router.navigate(['/login']), 1200);
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Reset failed';
        this.loading = false;
      }
    });
  }
}
