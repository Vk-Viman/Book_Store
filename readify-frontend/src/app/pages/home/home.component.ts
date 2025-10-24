import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  template: `
  <div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h3>Welcome to Readify</h3>
      <button class="btn btn-outline-danger" (click)="logout()">Logout</button>
    </div>
    <div class="alert alert-info">You're logged in. Build your dashboard here.</div>
  </div>
  `
})
export class HomeComponent implements OnInit {
  constructor(private router: Router, private auth: AuthService) {}

  ngOnInit() {
    // Redirect users to the catalog so they see products after login
    const target = '/books';
    this.router.navigateByUrl(target);
  }

  logout() {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
