import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
  <div class="login-container">
    <mat-card class="login-card">
      <mat-card-header>
        <mat-card-title class="text-center w-100">
          <mat-icon class="logo-icon">book</mat-icon>
          <h2>Welcome to Readify</h2>
          <p class="subtitle">Sign in to continue</p>
        </mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="loginUser()">
          <mat-form-field appearance="outline" class="w-100 mb-3">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" required autocomplete="email">
            <mat-icon matPrefix>email</mat-icon>
            <mat-error *ngIf="form.get('email')?.hasError('required')">
              Email is required
            </mat-error>
            <mat-error *ngIf="form.get('email')?.hasError('email')">
              Please enter a valid email
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-100 mb-2">
            <mat-label>Password</mat-label>
            <input matInput [type]="hidePassword ? 'password' : 'text'" formControlName="password" required autocomplete="current-password">
            <mat-icon matPrefix>lock</mat-icon>
            <button mat-icon-button matSuffix (click)="hidePassword = !hidePassword" type="button" [attr.aria-label]="'Toggle password visibility'">
              <mat-icon>{{hidePassword ? 'visibility_off' : 'visibility'}}</mat-icon>
            </button>
            <mat-error *ngIf="form.get('password')?.hasError('required')">
              Password is required
            </mat-error>
          </mat-form-field>

          <div class="text-end mb-3">
            <a routerLink="/forgot-password" class="forgot-link">Forgot password?</a>
          </div>

          <button mat-raised-button color="primary" type="submit" class="w-100 mb-3" [disabled]="form.invalid || loading">
            <mat-spinner *ngIf="loading" diameter="20" class="me-2"></mat-spinner>
            <span *ngIf="!loading">Sign In</span>
            <span *ngIf="loading">Signing in...</span>
          </button>

          <mat-error *ngIf="error" class="text-center d-block mb-3">
            <mat-icon>error</mat-icon>
            {{ error }}
          </mat-error>

          <div class="text-center">
            <p class="text-muted">
              Don't have an account?
              <a routerLink="/register" class="register-link">Register here</a>
            </p>
            <p class="demo-credentials">
              <mat-icon class="small-icon">info</mat-icon>
              Demo: admin&#64;demo.com / Readify#Demo123!
            </p>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  </div>
  `,
  styles: [`
    .login-container {
      min-height: calc(100vh - 64px);
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
    }
    .login-card {
      max-width: 480px;
      width: 100%;
    }
    .logo-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--primary-color);
    }
    mat-card-title {
      margin-bottom: 8px;
    }
    .subtitle {
      color: var(--text-secondary);
      font-size: 1rem;
      font-weight: 400;
      margin: 0;
    }
    .forgot-link, .register-link {
      color: var(--primary-color);
      text-decoration: none;
      font-weight: 500;
    }
    .forgot-link:hover, .register-link:hover {
      text-decoration: underline;
    }
    .demo-credentials {
      background: #f5f5f5;
      padding: 8px;
      border-radius: 4px;
      font-size: 0.875rem;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 4px;
    }
    .small-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }
    mat-spinner {
      display: inline-block;
    }
  `]
})
export class LoginComponent {
  form: FormGroup;
  error = '';
  loading = false;
  hidePassword = true;
  private returnUrl = '/books';

  constructor(private auth: AuthService, private router: Router, private fb: FormBuilder) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  loginUser() {
    if (this.form.invalid) return;
    this.error = '';
    this.loading = true;
    this.auth.login(this.form.value).subscribe({
      next: (res: any) => {
        const token = res?.token ?? res?.Token;
        const refresh = res?.refresh ?? res?.Refresh;
        const role = res?.role ?? res?.Role ?? res?.roleName;
        if (token) {
          this.auth.setSession(token, refresh, role);
        }
        this.loading = false;
        const target = this.returnUrl && this.returnUrl !== '/home' ? this.returnUrl : '/books';
        this.router.navigateByUrl(target);
      },
      error: (err) => {
        this.error = err?.error?.message || 'Invalid email or password';
        this.loading = false;
      }
    });
  }
}
