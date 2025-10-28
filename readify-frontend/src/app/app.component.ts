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
    mat-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
    }
    .navbar-brand {
      font-size: 1.5rem;
      font-weight: 500;
    }
    .active {
      background-color: rgba(255, 255, 255, 0.1);
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
