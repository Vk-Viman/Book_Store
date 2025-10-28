import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { NotificationService } from './services/notification.service';
import { AuthService } from './services/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet, 
    CommonModule, 
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule
  ],
  templateUrl: './app.component.html',
  styles: [`
    .spacer {
      flex: 1 1 auto;
    }
    .main-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .navbar-brand {
      font-size: 1.5rem;
      font-weight: 500;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .brand-text {
      display: none;
    }
    @media (min-width: 576px) {
      .brand-text {
        display: inline;
      }
    }
    .nav-label {
      margin-left: 4px;
    }
    .active {
      background-color: rgba(255, 255, 255, 0.1);
      border-radius: 4px;
    }
    @media (max-width: 767px) {
      .nav-label {
        display: none;
      }
      .navbar-brand {
        font-size: 1.2rem;
      }
    }
  `]
})
export class AppComponent {
  notify = inject(NotificationService);
  private auth = inject(AuthService);
  private router = inject(Router);

  loggedIn$: Observable<boolean> = this.auth.isLoggedIn();
  isAdmin$: Observable<boolean> = this.auth.isAdmin$();

  logout() {
    this.auth.logout();
    this.notify.success('Logged out successfully');
    this.router.navigate(['/login']);
  }
}
