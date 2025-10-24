import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
  <div class="container mt-5" style="max-width: 400px;">
    <h3 class="text-center mb-4">Readify Login</h3>
    <form (ngSubmit)="loginUser()">
      <div class="form-group mb-3">
        <label>Email</label>
        <input type="email" class="form-control" [(ngModel)]="email" name="email" required>
      </div>
      <div class="form-group mb-3">
        <label>Password</label>
        <input type="password" class="form-control" [(ngModel)]="password" name="password" required>
      </div>
      <button type="submit" class="btn btn-primary w-100" [disabled]="loading">Login</button>
    </form>
    <div *ngIf="error" class="alert alert-danger mt-3">{{ error }}</div>
    <p class="mt-3 text-center">
      Don't have an account?
      <a routerLink="/register">Register</a>
    </p>
  </div>
  `,
  styles: []
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';
  loading = false;

  private returnUrl = '/books';

  constructor(private auth: AuthService, private router: Router, private route: ActivatedRoute) {
    const q = this.route.snapshot.queryParamMap.get('returnUrl');
    if (q) this.returnUrl = q;
  }

  loginUser() {
    this.error = '';
    this.loading = true;
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: (res: any) => {
        const token = res?.token ?? res?.Token;
        const refresh = res?.refresh ?? res?.Refresh;
        const role = res?.role ?? res?.Role ?? res?.roleName;
        if (token) {
          this.auth.setSession(token, refresh, role);
        }
        this.loading = false;
        // navigate then reload to ensure route/standalone components initialize with auth state
        const target = this.returnUrl && this.returnUrl !== '/home' ? this.returnUrl : '/books';
        this.router.navigateByUrl(target).then(() => window.location.reload());
      },
      error: (err) => {
        this.error = err?.error?.message || 'Invalid email or password';
        this.loading = false;
      }
    });
  }
}
