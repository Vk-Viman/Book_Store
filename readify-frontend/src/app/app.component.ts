import { Component } from '@angular/core';
import { RouterLink, RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet],
  template: `
  <div class="app-debug-banner" style="padding:8px; background:#f8f9fa; border-bottom:1px solid #e5e5e5">Readify App</div>
  <nav class="navbar navbar-expand navbar-light bg-light px-3">
    <a class="navbar-brand" routerLink="/books">Readify</a>
    <ul class="navbar-nav ms-auto">
      <li class="nav-item" *ngIf="!loggedInFlag"><a class="nav-link" routerLink="/login">Login</a></li>
      <li class="nav-item" *ngIf="!loggedInFlag"><a class="nav-link" routerLink="/register">Register</a></li>
      <li class="nav-item" *ngIf="loggedInFlag"><a class="nav-link" routerLink="/books">Home</a></li>
      <li class="nav-item" *ngIf="isAdminFlag"><a class="nav-link" routerLink="/admin/home">Admin</a></li>
      <li class="nav-item" *ngIf="loggedInFlag"><a class="nav-link" (click)="logout()">Logout</a></li>
    </ul>
  </nav>
  <div style="padding:16px">
    <router-outlet></router-outlet>
  </div>
  `,
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'readify-frontend';
  loggedInFlag = false;
  isAdminFlag = false;

  constructor(private auth: AuthService, private router: Router) {
    this.loggedInFlag = !!this.auth.getToken() && !this.auth.isTokenExpired(this.auth.getToken());
    this.isAdminFlag = this.auth.isAdmin();

    this.auth.isLoggedIn().subscribe(v => this.loggedInFlag = v);
    this.auth.isAdmin$().subscribe(v => this.isAdminFlag = v);
  }

  logout() { this.auth.logout(); this.router.navigate(['/login']); }
}
