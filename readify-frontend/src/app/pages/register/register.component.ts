import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators, ValidationErrors, AbstractControl, FormGroup } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { environment } from '../../../environments/environment';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirm')?.value;
  return password === confirm ? null : { mismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
  <div class="container mt-5" style="max-width: 450px;">
    <h3 class="text-center mb-4">Create your Readify account</h3>
    <form [formGroup]="form" (ngSubmit)="registerUser()">
      <div class="form-group mb-3">
        <label>Full name</label>
        <input type="text" class="form-control" formControlName="fullName" required>
        <div *ngIf="form.get('fullName')?.touched && form.get('fullName')?.invalid" class="text-danger small">Full name is required</div>
      </div>
      <div class="form-group mb-3">
        <label>Email</label>
        <input type="email" class="form-control" formControlName="email" required>
        <div *ngIf="form.get('email')?.touched && form.get('email')?.invalid" class="text-danger small">Valid email is required</div>
      </div>
      <div class="form-group mb-3">
        <label>Password</label>
        <input type="password" class="form-control" formControlName="password" required>
        <div *ngIf="form.get('password')?.touched && form.get('password')?.hasError('minlength')" class="text-danger small">Minimum 8 characters</div>
      </div>
      <div class="form-group mb-3">
        <label>Confirm Password</label>
        <input type="password" class="form-control" formControlName="confirm" required>
        <div *ngIf="form.errors?.['mismatch'] && form.get('confirm')?.touched" class="text-danger small">Passwords do not match</div>
      </div>
      <button type="submit" class="btn btn-success w-100" [disabled]="form.invalid || loading">Register</button>
    </form>
    <div *ngIf="message" class="alert alert-success mt-3">{{ message }}</div>
    <div *ngIf="error" class="alert alert-danger mt-3">{{ error }}</div>
    <p class="mt-3 text-center">
      Already have an account?
      <a routerLink="/login">Login</a>
    </p>
  </div>
  `,
  styles: []
})
export class RegisterComponent {
  form!: FormGroup;

  loading = false;
  error = '';
  message = '';

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', Validators.required]
    }, { validators: passwordsMatch });
  }

  private formatError(err: any): string {
    // Prefer structured backend error
    const msg = err?.error?.message || err?.message;
    if (msg) return msg;
    // If backend returned a string body
    if (typeof err?.error === 'string') return err.error;
    // Network/CORS/Backend down
    if (err?.status === 0) return `Cannot reach API at ${environment.apiUrl}. Is the backend running?`;
    // Fallback to full object
    try { return JSON.stringify(err?.error ?? err); } catch { return 'Registration failed'; }
  }

  registerUser() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';
    const { fullName, email, password } = this.form.value;
    this.auth.register({ fullName, email, password }).subscribe({
      next: (res: any) => {
        const token = res?.token ?? res?.Token;
        const refresh = res?.refresh ?? res?.Refresh;
        const role = res?.role ?? res?.Role ?? res?.roleName;
        if (token) {
          this.auth.setSession(token, refresh, role);
        }
        this.router.navigate(['/books']);
      },
      error: (err) => {
        console.error('Registration error', err);
        this.error = this.formatError(err);
        this.loading = false;
      }
    });
  }
}
