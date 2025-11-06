import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatCardModule, MatIconModule],
  template: `
    <div class="home-hero container mt-5">
      <mat-card class="hero-card">
        <div class="hero-inner d-flex flex-column flex-md-row align-items-center gap-4 p-4">
          <div class="hero-copy flex-fill">
            <h1>Readify — Your friendly neighborhood bookstore</h1>
            <p class="lead">Discover thoughtfully curated books across technology, business, and fiction. Fast checkout, easy returns, and personalized recommendations.</p>
            <div class="mt-3">
              <a routerLink="/books" class="btn btn-primary btn-lg me-2" role="button" aria-label="Browse books">Browse Books</a>
              <a routerLink="/login" class="btn btn-outline-secondary btn-lg" role="button" aria-label="Login">Login</a>
              <a routerLink="/register" class="btn btn-outline-secondary btn-lg ms-2" role="button" aria-label="Register">Register</a>
            </div>
          </div>

          <div class="hero-visual d-none d-md-block text-center" aria-hidden="true">
            <mat-icon style="font-size:96px;color:var(--primary-color)">menu_book</mat-icon>
            <div class="mt-2 text-muted">Thousands of titles</div>
          </div>
        </div>
      </mat-card>

      <div class="features mt-4 row">
        <div class="col-md-4 mb-3">
          <mat-card>
            <mat-card-title>Fast Checkout</mat-card-title>
            <mat-card-content>Secure payments and quick order processing so you get your books fast.</mat-card-content>
          </mat-card>
        </div>
        <div class="col-md-4 mb-3">
          <mat-card>
            <mat-card-title>Curated Selection</mat-card-title>
            <mat-card-content>Hand-picked books across popular topics and timeless classics.</mat-card-content>
          </mat-card>
        </div>
        <div class="col-md-4 mb-3">
          <mat-card>
            <mat-card-title>Easy Returns</mat-card-title>
            <mat-card-content>Hassle-free returns within 30 days.</mat-card-content>
          </mat-card>
        </div>
      </div>

    </div>
  `,
  styles: [`
    .hero-card { background: linear-gradient(180deg, #ffffff 0%, #f9fbff 100%); }
    .hero-inner h1 { margin: 0 0 8px 0; font-size: 2rem; }
    .hero-inner .lead { color: var(--text-secondary); max-width: 560px; }
    .features mat-card { padding: 12px; }
  `]
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
